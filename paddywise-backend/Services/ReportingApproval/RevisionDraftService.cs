using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PaddyWise.Api.Agents.FieldCultivation;
using PaddyWise.Api.Agents.Shared;
using PaddyWise.Api.Data;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.PestDisease;

namespace PaddyWise.Api.Services.ReportingApproval;

public class RevisionDraftService : IRevisionDraftService
{
    public const string AgentName = "RevisionDraftAgent";
    public const string DefaultPlanFallback = "Please review this plan and provide feedback.";
    public const string DefaultReportFallback = "Please review this report and provide feedback.";

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ApplicationDbContext _context;
    private readonly ILlmClient _llm;
    private readonly ILogger<RevisionDraftService> _logger;

    public RevisionDraftService(
        ApplicationDbContext context,
        ILlmClient llm,
        ILogger<RevisionDraftService> logger)
    {
        _context = context;
        _llm = llm;
        _logger = logger;
    }

    public async Task<string> DraftPlanRevisionCommentAsync(int planId, CancellationToken ct = default)
    {
        var plan = await _context.CultivationPlans
            .AsNoTracking()
            .Include(p => p.CultivationCycle)
                .ThenInclude(c => c.Field)
            .Include(p => p.CultivationCycle)
                .ThenInclude(c => c.Variety)
            .FirstOrDefaultAsync(p => p.Id == planId, ct);

        if (plan == null)
        {
            return DefaultPlanFallback;
        }

        var hasPlanJson = !string.IsNullOrWhiteSpace(plan.PlanJson) && plan.PlanJson.Trim() != "{}" && plan.PlanJson.Trim() != "[]";
        var hasValidationErrors = !string.IsNullOrWhiteSpace(plan.ValidationErrorsJson) && plan.ValidationErrorsJson.Trim() != "[]" && plan.ValidationErrorsJson.Trim() != "{}";

        if (!hasPlanJson && !hasValidationErrors)
        {
            return DefaultPlanFallback;
        }

        CultivationPlanOutput? planOutput = null;
        if (hasPlanJson)
        {
            try
            {
                planOutput = JsonSerializer.Deserialize<CultivationPlanOutput>(plan.PlanJson, JsonOptions);
            }
            catch (JsonException)
            {
                // Fall back to raw content if needed
            }
        }

        var validationErrors = hasValidationErrors
            ? ExtractValidationErrors(plan.ValidationErrorsJson!)
            : new List<string>();

        var cycle = plan.CultivationCycle;
        var varietyName = cycle?.Variety?.Name ?? "Paddy";
        var durationDays = cycle?.Variety != null ? cycle.Variety.DurationDays.ToString() : "standard";
        var fieldName = cycle?.Field?.Name ?? "Field";
        var fieldArea = cycle?.Field != null ? cycle.Field.Area.ToString("0.0") : "N/A";
        var soilType = cycle?.Field?.SoilType ?? "Not specified";
        var irrigationType = cycle?.Field?.IrrigationType ?? "Rainfed/Irrigated";
        var season = cycle != null ? $"{cycle.Season} {cycle.Year}" : "Current Season";

        var summary = planOutput?.Summary ?? "Stage-by-stage cultivation plan.";
        var stepHighlights = planOutput?.Steps != null && planOutput.Steps.Count > 0
            ? string.Join("; ", planOutput.Steps.Take(6).Select(s => $"{s.Stage} ({s.Category}): {s.Task}"))
            : "Standard land prep, water, nutrient, and harvest steps";

        var validationSection = validationErrors.Count > 0
            ? $"• Automated Validation Issues:\n" + string.Join("\n", validationErrors.Select(e => $"  - {e}"))
            : "• Automated Validation: Plan passed all stage timeline rules.";

        var correlationId = Guid.NewGuid().ToString("N");
        var inputJson = JsonSerializer.Serialize(new
        {
            objective = plan.Objective,
            variety = varietyName,
            field = fieldName,
            validationErrors,
            stepCount = planOutput?.Steps.Count ?? 0
        }, JsonOptions);

        var systemPrompt = "You are an expert Agricultural Extension Officer assistant for Sri Lankan paddy farming. " +
                           "Your role is to provide a balanced agronomic evaluation (pros, cons/risks, and actionable recommendations) " +
                           "to help the reviewing officer evaluate the cultivation plan and give constructive feedback to the farmer.";

        var userPrompt = $"""
            Analyze the following paddy cultivation plan for the reviewing Agricultural Officer:

            Farmer's Objective: "{plan.Objective}"
            Variety: {varietyName} ({durationDays} days)
            Field: {fieldName} ({fieldArea} acres, Soil: {soilType}, Irrigation: {irrigationType})
            Season: {season}
            Plan Summary: {summary}
            Key Planned Steps: {stepHighlights}
            {validationSection}

            Provide a concise agronomic evaluation formatted with clear bullet points (keep under 750 characters total so it fits the review comment box):
            • Pros: 1-2 key strengths of the plan.
            • Cons / Risks: 1-2 practical risks or challenges (e.g. input reduction impact, water/nutrient timing, disease risks).
            • Recommendation: 1-2 actionable, practical suggestions from the officer to the farmer.

            Use clear, respectful, plain language without markdown bolding.
            """;

        var stopwatch = Stopwatch.StartNew();
        string rawOutput = string.Empty;
        bool success = false;
        string? error = null;
        string draftResult = DefaultPlanFallback;

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

            stopwatch.Stop();

            if (!string.IsNullOrWhiteSpace(reply))
            {
                rawOutput = reply.Trim();
                draftResult = rawOutput;
                success = true;
            }
            else
            {
                error = "LLM returned an empty response.";
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Draft revision comment generation failed for plan {PlanId} (correlation {CorrelationId}).", planId, correlationId);
            error = ex.Message;
            draftResult = DefaultPlanFallback;
        }

        try
        {
            _context.AgentRunLogs.Add(new AgentRunLog
            {
                CultivationPlanId = planId,
                AgentName = AgentName,
                CorrelationId = correlationId,
                InputJson = inputJson,
                ToolCallsJson = "[]",
                RawOutput = rawOutput,
                Success = success,
                Error = error,
                DurationMs = (int)stopwatch.ElapsedMilliseconds,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist AgentRunLog for plan {PlanId} (correlation {CorrelationId}).", planId, correlationId);
        }

        return draftResult;
    }

    public async Task<string> DraftReportRevisionCommentAsync(int reportId, CancellationToken ct = default)
    {
        var report = await _context.PestDiseaseReports
            .AsNoTracking()
            .Include(r => r.CropObservation)
            .FirstOrDefaultAsync(r => r.Id == reportId, ct);

        if (report == null || report.CropObservation == null)
        {
            return DefaultReportFallback;
        }

        var symptoms = report.CropObservation.Symptoms;
        if (string.IsNullOrWhiteSpace(symptoms) && string.IsNullOrWhiteSpace(report.PossibleIssue))
        {
            return DefaultReportFallback;
        }

        var correlationId = Guid.NewGuid().ToString("N");
        var inputJson = JsonSerializer.Serialize(new
        {
            reportId = report.Id,
            observationId = report.CropObservationId,
            possibleIssue = report.PossibleIssue,
            confidence = report.Confidence,
            symptoms = report.CropObservation.Symptoms,
            cropStage = report.CropObservation.CropStage.ToString(),
            severity = report.CropObservation.Severity.ToString()
        }, JsonOptions);

        var systemPrompt = "You are an expert Agricultural Extension Officer assistant for Sri Lankan paddy farming. " +
                           "Your role is to provide a balanced diagnostic evaluation (findings, risks/uncertainties, and recommendations) " +
                           "for a crop pest or disease report.";

        var userPrompt = $"""
            Evaluate this pest or disease report for the reviewing Agricultural Officer:

            Crop Observation:
            - Symptoms: "{report.CropObservation.Symptoms}"
            - Crop Stage: {report.CropObservation.CropStage}
            - Severity: {report.CropObservation.Severity}
            - AI Suggested Diagnosis: "{report.PossibleIssue}" (Confidence: {report.Confidence:P0})

            Provide a concise evaluation formatted with clear bullet points (keep under 750 characters total):
            • Pros / Findings: 1-2 observations that support or align with this diagnosis.
            • Cons / Risks: 1-2 uncertainties, alternate possibilities, or immediate crop damage risks.
            • Recommendation: 1-2 actionable, safe management steps or follow-ups for the farmer.

            Use clear, respectful, plain language without markdown bolding.
            """;

        var stopwatch = Stopwatch.StartNew();
        string rawOutput = string.Empty;
        bool success = false;
        string? error = null;
        string draftResult = DefaultReportFallback;

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

            stopwatch.Stop();

            if (!string.IsNullOrWhiteSpace(reply))
            {
                rawOutput = reply.Trim();
                draftResult = rawOutput;
                success = true;
            }
            else
            {
                error = "LLM returned an empty response.";
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Draft revision comment generation failed for report {ReportId} (correlation {CorrelationId}).", reportId, correlationId);
            error = ex.Message;
            draftResult = DefaultReportFallback;
        }

        try
        {
            _context.DiagnosisRunLogs.Add(new DiagnosisRunLog
            {
                CropObservationId = report.CropObservationId,
                AgentName = AgentName,
                CorrelationId = correlationId,
                InputJson = inputJson,
                ToolCallsJson = "[]",
                RawOutput = rawOutput,
                Success = success,
                Error = error,
                DurationMs = (int)stopwatch.ElapsedMilliseconds,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist DiagnosisRunLog for report {ReportId} (correlation {CorrelationId}).", reportId, correlationId);
        }

        return draftResult;
    }

    private static List<string> ExtractValidationErrors(string validationErrorsJson)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<List<string>>(validationErrorsJson, JsonOptions);
            if (parsed != null)
            {
                return parsed.Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
            }
        }
        catch (JsonException)
        {
            // Not a JSON array of strings — fallback below
        }

        var trimmed = validationErrorsJson.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed == "[]" || trimmed == "{}")
        {
            return new List<string>();
        }

        return new List<string> { trimmed };
    }
}
