using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaddyWise.Api.Agents.FieldCultivation;
using PaddyWise.Api.Agents.Shared;
using PaddyWise.Api.Data;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.PestDisease;

namespace PaddyWise.Api.Services.ReportingApproval.Agents;

public class ValidationAgentService : IValidationAgentService
{
    public const string AgentName = "ValidationAgent";
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly Regex DosagePattern = new(
        @"\d+(\.\d+)?\s?(kg|g|ml|l|litre|liter|bag|bags)\b|\d+(\.\d+)?\s?%",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly ApplicationDbContext _context;
    private readonly ILlmClient _llm;
    private readonly ILogger<ValidationAgentService> _logger;

    public ValidationAgentService(
        ApplicationDbContext context,
        ILlmClient llm,
        ILogger<ValidationAgentService> logger)
    {
        _context = context;
        _llm = llm;
        _logger = logger;
    }

    public async Task<AgentValidationResult> ValidatePestDiseaseReportAsync(int reportId, CancellationToken ct = default)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var stopwatch = Stopwatch.StartNew();

        var report = await _context.PestDiseaseReports
            .Include(r => r.CropObservation)
            .FirstOrDefaultAsync(r => r.Id == reportId, ct);

        if (report == null)
        {
            return new AgentValidationResult
            {
                IsValid = false,
                Violations = new List<string> { $"PestDiseaseReport with ID {reportId} not found." },
                RequiresOfficerReview = false
            };
        }

        var inputJson = JsonSerializer.Serialize(new
        {
            reportId = report.Id,
            observationId = report.CropObservationId,
            possibleIssue = report.PossibleIssue,
            confidence = report.Confidence
        }, JsonOptions);

        var violations = new List<string>();

        // STEP 1 — Deterministic checks
        // 1. Confidence check
        if (report.Confidence < 0.00m || report.Confidence > 1.00m)
        {
            violations.Add($"Confidence score {report.Confidence} is invalid. It must be between 0.00 and 1.00.");
        }

        // 2. Knowledge base check (controlled tool call against PestDiseaseKnowledgeEntries)
        if (string.IsNullOrWhiteSpace(report.PossibleIssue))
        {
            violations.Add("Possible issue name is empty or missing.");
        }
        else
        {
            var trimmedIssue = report.PossibleIssue.Trim().ToLower();
            var exists = await _context.PestDiseaseKnowledgeEntries
                .AnyAsync(k => k.Name.ToLower() == trimmedIssue, ct);

            if (!exists)
            {
                violations.Add($"Possible issue '{report.PossibleIssue}' was not found in the official pest/disease knowledge base.");
            }
        }

        var isValid = violations.Count == 0;
        var validationResult = new AgentValidationResult
        {
            IsValid = isValid,
            Violations = violations,
            RequiresOfficerReview = isValid
        };

        string? explanation = null;

        // STEP 2 & 3 — Apply result and optional LLM explanation
        if (isValid)
        {
            report.Status = PestDiseaseReportStatus.PendingOfficerReview;
        }
        else
        {
            report.Status = PestDiseaseReportStatus.Rejected;

            explanation = await GenerateExplanationAsync(
                targetType: "Pest & Disease Report",
                contextInfo: $"Report #{report.Id}, Issue: '{report.PossibleIssue}', Confidence: {report.Confidence:P0}",
                violations: violations,
                correlationId: correlationId,
                ct: ct);

            report.OfficerComment = explanation;
        }

        await _context.SaveChangesAsync(ct);
        stopwatch.Stop();

        // STEP 4 — Log run to DiagnosisRunLogs
        try
        {
            _context.DiagnosisRunLogs.Add(new DiagnosisRunLog
            {
                CropObservationId = report.CropObservationId,
                AgentName = AgentName,
                CorrelationId = correlationId,
                InputJson = inputJson,
                ToolCallsJson = JsonSerializer.Serialize(new[]
                {
                    new { tool = "check_knowledge_base", parameter = report.PossibleIssue }
                }, JsonOptions),
                RawOutput = JsonSerializer.Serialize(new
                {
                    isValid = validationResult.IsValid,
                    violations = validationResult.Violations,
                    requiresOfficerReview = validationResult.RequiresOfficerReview,
                    explanation
                }, JsonOptions),
                Success = validationResult.IsValid,
                Error = validationResult.IsValid ? null : string.Join("; ", violations),
                DurationMs = (int)stopwatch.ElapsedMilliseconds,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist DiagnosisRunLog for report {ReportId} (correlation {CorrelationId}).", report.Id, correlationId);
        }

        return validationResult;
    }

    public async Task<AgentValidationResult> ValidateCultivationPlanAsync(int planId, CancellationToken ct = default)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var stopwatch = Stopwatch.StartNew();

        var plan = await _context.CultivationPlans
            .FirstOrDefaultAsync(p => p.Id == planId, ct);

        if (plan == null)
        {
            return new AgentValidationResult
            {
                IsValid = false,
                Violations = new List<string> { $"CultivationPlan with ID {planId} not found." },
                RequiresOfficerReview = false
            };
        }

        var inputJson = JsonSerializer.Serialize(new
        {
            planId = plan.Id,
            cycleId = plan.CultivationCycleId,
            objective = plan.Objective
        }, JsonOptions);

        var violations = new List<string>();

        // STEP 1 — Deterministic checks
        CultivationPlanOutput? planOutput = null;

        if (string.IsNullOrWhiteSpace(plan.PlanJson) || plan.PlanJson.Trim() == "{}" || plan.PlanJson.Trim() == "[]")
        {
            violations.Add("PlanJson is missing or empty.");
        }
        else
        {
            try
            {
                planOutput = JsonSerializer.Deserialize<CultivationPlanOutput>(plan.PlanJson, CultivationPlanJson.Options);
            }
            catch (JsonException ex)
            {
                violations.Add($"PlanJson is not valid structured data: {ex.Message}");
            }

            if (planOutput == null)
            {
                violations.Add("PlanJson could not be deserialized into expected CultivationPlanOutput shape.");
            }
            else
            {
                // Validate expected structure shape
                if (planOutput.Steps == null)
                {
                    violations.Add("Plan is missing the 'steps' collection.");
                }
                else if (planOutput.Steps.Count == 0)
                {
                    violations.Add("Plan contains an empty 'steps' list.");
                }

                if (planOutput.Assumptions == null)
                {
                    violations.Add("Plan is missing the 'assumptions' collection.");
                }

                if (planOutput.Delegations == null)
                {
                    violations.Add("Plan is missing the 'delegations' collection.");
                }

                // RULE CHECK: scan Steps/Summary for numeric dosage-like values
                if (!string.IsNullOrWhiteSpace(planOutput.Summary))
                {
                    var summaryMatch = DosagePattern.Match(planOutput.Summary);
                    if (summaryMatch.Success)
                    {
                        violations.Add($"Plan summary contains prohibited numeric dosage: '{summaryMatch.Value.Trim()}'. Quantities must be determined by the Resource Analysis agent.");
                    }
                }

                if (planOutput.Steps != null)
                {
                    for (int i = 0; i < planOutput.Steps.Count; i++)
                    {
                        var step = planOutput.Steps[i];
                        var stepNum = i + 1;

                        if (string.IsNullOrWhiteSpace(step.Task))
                        {
                            violations.Add($"Step {stepNum} has an empty or missing task description.");
                            continue;
                        }

                        var taskMatch = DosagePattern.Match(step.Task);
                        if (taskMatch.Success)
                        {
                            violations.Add($"Step {stepNum} contains prohibited numeric dosage ('{taskMatch.Value.Trim()}') in task description. Quantities belong to the Resource Analysis agent.");
                        }

                        if (!string.IsNullOrWhiteSpace(step.Rationale))
                        {
                            var rationaleMatch = DosagePattern.Match(step.Rationale);
                            if (rationaleMatch.Success)
                            {
                                violations.Add($"Step {stepNum} contains prohibited numeric dosage ('{rationaleMatch.Value.Trim()}') in rationale. Quantities belong to the Resource Analysis agent.");
                            }
                        }
                    }
                }
            }
        }

        var isValid = violations.Count == 0;
        var validationResult = new AgentValidationResult
        {
            IsValid = isValid,
            Violations = violations,
            RequiresOfficerReview = isValid
        };

        string? explanation = null;

        // STEP 2 & 3 — Apply result and optional LLM explanation
        if (isValid)
        {
            plan.Status = PlanStatus.PendingOfficerApproval;
            plan.ValidationErrorsJson = null;
        }
        else
        {
            plan.Status = PlanStatus.ValidationFailed;
            plan.ValidationErrorsJson = JsonSerializer.Serialize(violations, JsonOptions);

            explanation = await GenerateExplanationAsync(
                targetType: "Cultivation Plan",
                contextInfo: $"Plan #{plan.Id}, Objective: '{plan.Objective}'",
                violations: violations,
                correlationId: correlationId,
                ct: ct);

            plan.OfficerComment = explanation;
        }

        plan.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        stopwatch.Stop();

        // STEP 4 — Log run to AgentRunLogs
        try
        {
            _context.AgentRunLogs.Add(new AgentRunLog
            {
                CultivationPlanId = plan.Id,
                AgentName = AgentName,
                CorrelationId = correlationId,
                InputJson = inputJson,
                ToolCallsJson = JsonSerializer.Serialize(new[]
                {
                    new { tool = "validate_plan_rules", stepCount = planOutput?.Steps?.Count ?? 0 }
                }, JsonOptions),
                RawOutput = JsonSerializer.Serialize(new
                {
                    isValid = validationResult.IsValid,
                    violations = validationResult.Violations,
                    requiresOfficerReview = validationResult.RequiresOfficerReview,
                    explanation
                }, JsonOptions),
                Success = validationResult.IsValid,
                Error = validationResult.IsValid ? null : string.Join("; ", violations),
                DurationMs = (int)stopwatch.ElapsedMilliseconds,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist AgentRunLog for plan {PlanId} (correlation {CorrelationId}).", plan.Id, correlationId);
        }

        return validationResult;
    }

    private async Task<string> GenerateExplanationAsync(
        string targetType,
        string contextInfo,
        List<string> violations,
        string correlationId,
        CancellationToken ct)
    {
        var fallbackText = string.Join("; ", violations);

        var systemPrompt = "You are an automated Validation Agent for Sri Lankan paddy farming operations. " +
                           "Your role is to explain rule violations in automated agent outputs in clear, professional language " +
                           "so the reviewing Agricultural Extension Officer understands immediately why it failed.";

        var violationsList = string.Join("\n", violations.Select(v => $"• {v}"));
        var userPrompt = $"""
            The following {targetType} failed automated deterministic validation:
            Target: {contextInfo}

            Validation Violations:
            {violationsList}

            Provide a concise, plain-language explanation of these violations for the Agricultural Officer.
            Explain what failed, why it violates domain rules, and what should be corrected.
            Keep under 500 characters, clear and respectful, without markdown bolding or asterisks.
            """;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(DefaultTimeout);

            var reply = await _llm.CompleteJsonAsync(
                systemPrompt,
                userPrompt,
                Array.Empty<LlmToolDefinition>(),
                (_, _) => Task.FromResult("{}"),
                cts.Token);

            if (!string.IsNullOrWhiteSpace(reply))
            {
                return reply.Trim();
            }

            return fallbackText;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM explanation generation failed for correlation {CorrelationId}. Falling back to violations list.", correlationId);
            return fallbackText;
        }
    }
}
