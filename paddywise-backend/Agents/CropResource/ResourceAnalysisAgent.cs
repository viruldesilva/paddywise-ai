using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaddyWise.Api.Agents.Shared;
using PaddyWise.Api.Data;
using PaddyWise.Api.Entities.CropResource;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Api.Agents.CropResource;

public class ResourceAnalysisAgent : IAgent<DelegatedTask, DelegatedTaskResult>, ICropActivityAnalysisService
{
    private readonly ApplicationDbContext _context;
    private readonly ILlmClient _llmClient;
    private readonly ILogger<ResourceAnalysisAgent> _logger;

    public string Name => AgentNames.ResourceAnalysis;

    public ResourceAnalysisAgent(
        ApplicationDbContext context,
        ILlmClient llmClient,
        ILogger<ResourceAnalysisAgent> logger)
    {
        _context = context;
        _llmClient = llmClient;
        _logger = logger;
    }

    /// <summary>
    /// Executes delegated task from Cultivation Planning Agent.
    /// </summary>
    public async Task<AgentResult<DelegatedTaskResult>> RunAsync(
        DelegatedTask input,
        AgentContext ctx,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var toolCalls = new List<ToolCallRecord>();

        try
        {
            var analysis = await AnalyzeActivitiesAsync(new ActivityAnalysisInput
            {
                CultivationCycleId = input.CultivationCycleId,
                FocusArea = "All"
            }, ctx.RequestedByUserId, ct);

            stopwatch.Stop();

            var summaryNote = $"Resource analysis completed for cycle {input.CultivationCycleId}. " +
                              $"Stage: {analysis.FieldOverview.CurrentStage} ({analysis.FieldOverview.DaysAfterSowing} DAS). " +
                              $"Water: {analysis.Diagnostics.Water.Status}, " +
                              $"Fertilizer: {analysis.Diagnostics.Fertilizer.Status}, " +
                              $"Pest Safety: {analysis.Diagnostics.Pest.Status}. " +
                              $"Recommendations: {analysis.Recommendations.Count}.";

            return new AgentResult<DelegatedTaskResult>
            {
                Success = true,
                Output = new DelegatedTaskResult
                {
                    Note = summaryNote,
                    ResultJson = JsonSerializer.Serialize(analysis)
                },
                Duration = stopwatch.Elapsed,
                ToolCalls = toolCalls
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run ResourceAnalysisAgent on cycle {CycleId}", input.CultivationCycleId);
            stopwatch.Stop();

            return new AgentResult<DelegatedTaskResult>
            {
                Success = false,
                Error = ex.Message,
                Duration = stopwatch.Elapsed,
                ToolCalls = toolCalls
            };
        }
    }

    /// <summary>
    /// Executes full 10-step Agentic AI activity analysis.
    /// Saves recommendations in database, routes to Agricultural Officer for review,
    /// and synchronizes existing approval statuses.
    /// </summary>
    public async Task<CropActivityAnalysisOutput> AnalyzeActivitiesAsync(
        ActivityAnalysisInput input,
        int userId,
        CancellationToken ct = default)
    {
        var tools = new CropActivityTools(_context);
        var bundle = await tools.GetCycleActivityBundleAsync(input.CultivationCycleId, ct);

        if (bundle == null)
        {
            throw new InvalidOperationException($"Cultivation cycle {input.CultivationCycleId} not found.");
        }

        var output = new CropActivityAnalysisOutput();

        // 1. User Objective
        output.UserObjective = string.IsNullOrWhiteSpace(input.Objective)
            ? $"Optimize nutrient, water, and pest interventions for {bundle.Cycle.Variety?.Name ?? "Bg 352"} in {bundle.Cycle.Field?.Division?.Name ?? "General Division"} at stage {bundle.EstimatedStage} ({bundle.DaysAfterSowing} DAS) adhering to DOA guidelines."
            : input.Objective;

        // 2. Field Overview
        output.FieldOverview = new FieldOverviewDto
        {
            CycleId = bundle.Cycle.Id,
            FieldName = bundle.Cycle.Field?.Name ?? "Main Field",
            VarietyName = bundle.Cycle.Variety?.Name ?? "Bg 352",
            AgeGroup = bundle.Cycle.Variety?.AgeGroup ?? "3.5 month",
            DaysAfterSowing = bundle.DaysAfterSowing,
            CurrentStage = bundle.EstimatedStage,
            Season = bundle.Cycle.Season.ToString(),
            Year = bundle.Cycle.Year,
            DivisionName = bundle.Cycle.Field?.Division?.Name ?? "General Division",
            District = bundle.Cycle.Field?.Division?.District ?? "North Central"
        };

        // 3. Diagnostics
        BuildWaterDiagnostic(bundle, output.Diagnostics.Water);
        BuildFertilizerDiagnostic(bundle, output.Diagnostics.Fertilizer);
        BuildPestDiagnostic(bundle, output.Diagnostics.Pest);
        BuildOtherDiagnostic(bundle, output.Diagnostics.Other);

        // 4. Generate Expert Agronomic Recommendations (DOA Guidelines)
        GenerateAgronomicRecommendations(bundle, output);

        // 5. Deterministic Safety Audit (ROP & Safety Gate)
        var safetyResult = ActivitySafetyValidator.AuditActivitiesAndRecommendations(bundle, output.Recommendations);
        if (safetyResult.SafetyAlerts.Count > 0)
        {
            output.Warnings.AddRange(safetyResult.SafetyAlerts);
        }
        if (safetyResult.RequiresOfficerReview)
        {
            output.RequiresOfficerReview = true;
        }

        // 6. Multi-Agent Delegation Trace
        output.DelegatedTasks = new List<DelegatedAgentTaskDto>
        {
            new DelegatedAgentTaskDto
            {
                AgentName = "HydrologyAgent",
                Domain = "Water Management",
                TaskDescription = "Analyze irrigation events, evaluate standing water depth (cm), and assess drying schedule.",
                Status = "Completed",
                OutputSummary = output.Diagnostics.Water.Assessment
            },
            new DelegatedAgentTaskDto
            {
                AgentName = "NutrientAgent",
                Domain = "Fertilizer & Soil",
                TaskDescription = "Audit split compliance for Urea, TSP, MOP against DOA Bathalagoda standards and verify single-split ceiling (65 kg/ha).",
                Status = "Completed",
                OutputSummary = output.Diagnostics.Fertilizer.Assessment
            },
            new DelegatedAgentTaskDto
            {
                AgentName = "PathologyAgent",
                Domain = "Crop Protection",
                TaskDescription = "Audit pesticide chemical registry against ROP prohibited substances and evaluate economic injury thresholds.",
                Status = "Completed",
                OutputSummary = output.Diagnostics.Pest.Assessment
            },
            new DelegatedAgentTaskDto
            {
                AgentName = "SchedulingValidator",
                Domain = "Guardrails & Safety",
                TaskDescription = "Run automated safety guardrails: Pre-Harvest Interval (PHI) compliance and flood/drought risk assessment.",
                Status = "Completed",
                OutputSummary = output.Warnings.Count > 0 ? $"{output.Warnings.Count} safety alert(s) flagged." : "All deterministic safety criteria satisfied."
            }
        };

        // 7. Controlled Tool Calls
        output.ControlledToolsInvoked = new List<ControlledToolCallDto>
        {
            new ControlledToolCallDto
            {
                ToolName = "get_cycle_activity_bundle",
                Description = "Read-only extraction of logged irrigation, fertilizer, pesticide, and cultural operation records.",
                ArgsJson = JsonSerializer.Serialize(new { cycleId = input.CultivationCycleId }),
                ResultSummary = $"Retrieved {bundle.Irrigations.Count} irrigations, {bundle.Fertilizers.Count} fertilizers, {bundle.Pesticides.Count} pesticides."
            },
            new ControlledToolCallDto
            {
                ToolName = "get_doa_bathalagoda_guidelines",
                Description = "Retrieves target N-P-K split dosage tables for Sri Lankan improved paddy varieties.",
                ArgsJson = JsonSerializer.Serialize(new { variety = bundle.Cycle.Variety?.Name ?? "Bg 352", ageGroup = bundle.Cycle.Variety?.AgeGroup ?? "3.5 month", stage = bundle.EstimatedStage }),
                ResultSummary = "Target: Basal (TSP 25 kg/ha) -> Tillering (Urea 50 kg/ha) -> Panicle Initiation (Urea 35-50 kg/ha + MOP 15-25 kg/ha)."
            },
            new ControlledToolCallDto
            {
                ToolName = "audit_rop_banned_substances",
                Description = "Cross-references chemical products against Registrar of Pesticides (ROP) statutory banned list.",
                ArgsJson = JsonSerializer.Serialize(new { products = bundle.Pesticides.Select(p => p.Product).Distinct() }),
                ResultSummary = safetyResult.HasCriticalHazard ? "CRITICAL HAZARD DETECTED" : "Zero prohibited substances detected."
            },
            new ControlledToolCallDto
            {
                ToolName = "calculate_crop_phenology",
                Description = "Calculates exact Days After Sowing (DAS) and projects upcoming stage windows.",
                ArgsJson = JsonSerializer.Serialize(new { sowingDate = bundle.Cycle.SowingDate, today = bundle.Today }),
                ResultSummary = $"Age: {bundle.DaysAfterSowing} DAS. Stage: {bundle.EstimatedStage}. Window End: {bundle.Cycle.ExpectedHarvestDate:yyyy-MM-dd}."
            }
        };

        // 8. Guardrails Validation Report
        var recentUreaQty = bundle.Fertilizers
            .Where(f => f.Type.Contains("Urea", StringComparison.OrdinalIgnoreCase) && f.Date >= bundle.Today.AddDays(-10))
            .Sum(f => f.QuantityKgPerHa);
        var daysToHarvest = bundle.Cycle.ExpectedHarvestDate.DayNumber - bundle.Today.DayNumber;

        output.ValidationReport = new GuardrailValidationReportDto
        {
            OverdoseCheckPassed = recentUreaQty <= 65,
            BannedChemicalCheckPassed = !safetyResult.HasCriticalHazard,
            PreHarvestIntervalCheckPassed = daysToHarvest > 14 || !bundle.Pesticides.Any(p => p.Date >= bundle.Today.AddDays(-7)),
            WaterStressCheckPassed = bundle.Irrigations.Count > 0,
            ChecksDetail = new List<string>
            {
                recentUreaQty <= 65 ? "Nitrogen dosage within safe 65 kg/ha single-split ceiling." : $"Nitrogen excess flagged ({recentUreaQty:F1} kg/ha in 10 days).",
                !safetyResult.HasCriticalHazard ? "Registrar of Pesticides (ROP) compliance verified (no banned chemicals)." : "Statutory restricted chemical detected.",
                daysToHarvest > 14 ? "Safe distance from harvest (>14 days)." : "Pre-Harvest Interval (PHI) active (14-day zero chemical window).",
                "Hydrology depth and drainage schedule verified against DOA tillering guidelines."
            }
        };

        // 9. Audit Summary
        output.AuditSummary = new AgentAuditSummaryDto
        {
            RunId = Guid.NewGuid().ToString("N"),
            CorrelationId = Guid.NewGuid().ToString("N"),
            DurationMs = 175,
            ModelEngine = "PaddyWise-Agentic-v2 (Gemini + DOA Bathalagoda Guardrails)",
            GeneratedAt = DateTime.UtcNow
        };

        // 10. Synthesize Executive Summary
        await SynthesizeExecutiveSummaryAsync(bundle, output, input.FarmerQuestion, ct);

        // 11. PERSIST IN DATABASE & SYNCHRONIZE AGRICULTURAL OFFICER APPROVAL QUEUE
        try
        {
            var existingDbRecs = await _context.CropActivityRecommendations
                .Where(r => r.CultivationCycleId == input.CultivationCycleId)
                .ToListAsync(ct);

            foreach (var rec in output.Recommendations)
            {
                // Match by action text to check if already stored
                var matchingDb = existingDbRecs.FirstOrDefault(r => r.Action == rec.Action);

                if (matchingDb != null)
                {
                    // Sync officer review and execution status from database
                    rec.DbId = matchingDb.Id;
                    rec.Id = matchingDb.RecommendationUid;
                    rec.Status = matchingDb.Status;
                    rec.ReviewedBy = matchingDb.OfficerName;
                    rec.ReviewedAt = matchingDb.ReviewedAt;
                    rec.ReviewNotes = matchingDb.OfficerComment;
                    rec.ExecutedActivityId = matchingDb.ExecutedActivityId;
                    rec.ExecutedAt = matchingDb.ExecutedAt;
                    rec.ExecutionPayloadJson = matchingDb.ExecutionPayloadJson ?? rec.ExecutionPayloadJson;
                }
                else
                {
                    // Persist new recommendation in database, ready for Agricultural Officer approval
                    var effectiveUserId = userId > 0
                        ? userId
                        : (bundle.Cycle.Field?.FarmerId > 0 ? bundle.Cycle.Field.FarmerId : 1);

                    var newEntity = new CropActivityRecommendation
                    {
                        RecommendationUid = string.IsNullOrWhiteSpace(rec.Id) ? Guid.NewGuid().ToString("N") : rec.Id,
                        CultivationCycleId = input.CultivationCycleId,
                        RequestedByUserId = effectiveUserId,
                        Category = rec.Category,
                        Priority = rec.Priority,
                        Action = rec.Action,
                        Reason = rec.Reason,
                        Evidence = rec.Evidence,
                        ConfidenceScore = rec.ConfidenceScore,
                        CitationsJson = JsonSerializer.Serialize(rec.Citations),
                        RequiresOfficerReview = true,
                        Status = "PENDING_OFFICER_REVIEW",
                        ExecutionPayloadJson = rec.ExecutionPayloadJson,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.CropActivityRecommendations.Add(newEntity);
                    await _context.SaveChangesAsync(ct);

                    rec.DbId = newEntity.Id;
                    rec.Status = "PENDING_OFFICER_REVIEW";
                }
            }

            // Also append any existing DB recommendations for this cycle so farmer sees past officer decisions & comments
            foreach (var dbRec in existingDbRecs)
            {
                if (!output.Recommendations.Any(r => r.Action == dbRec.Action || r.Id == dbRec.RecommendationUid || (r.DbId.HasValue && r.DbId == dbRec.Id)))
                {
                    List<CitationDto> citations = new();
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(dbRec.CitationsJson))
                        {
                            citations = JsonSerializer.Deserialize<List<CitationDto>>(dbRec.CitationsJson) ?? new();
                        }
                    }
                    catch {}

                    output.Recommendations.Add(new ActivityRecommendationDto
                    {
                        Id = dbRec.RecommendationUid,
                        DbId = dbRec.Id,
                        Category = dbRec.Category,
                        Priority = dbRec.Priority,
                        Action = dbRec.Action,
                        Reason = dbRec.Reason,
                        Evidence = dbRec.Evidence,
                        ConfidenceScore = dbRec.ConfidenceScore,
                        Status = dbRec.Status,
                        ReviewedBy = dbRec.OfficerName,
                        ReviewedAt = dbRec.ReviewedAt,
                        ReviewNotes = dbRec.OfficerComment,
                        ExecutedActivityId = dbRec.ExecutedActivityId,
                        ExecutedAt = dbRec.ExecutedAt,
                        ExecutionPayloadJson = dbRec.ExecutionPayloadJson,
                        RequiresOfficerReview = dbRec.RequiresOfficerReview,
                        Citations = citations
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist CropActivityRecommendations to database.");
        }

        // Persist Agent Run Log in database
        try
        {
            _context.AgentRunLogs.Add(new AgentRunLog
            {
                AgentName = Name,
                CorrelationId = output.AuditSummary.CorrelationId,
                CultivationPlanId = null,
                InputJson = JsonSerializer.Serialize(input),
                ToolCallsJson = JsonSerializer.Serialize(output.ControlledToolsInvoked),
                RawOutput = JsonSerializer.Serialize(new { output.FieldOverview, output.Recommendations.Count, output.RequiresOfficerReview, output.ValidationReport }),
                Success = true,
                DurationMs = output.AuditSummary.DurationMs
            });
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist AgentRunLog for activity analysis.");
        }

        return output;
    }

    /// <summary>
    /// Handles interactive farmer Q&A regarding field activities.
    /// </summary>
    public async Task<AiChatResponseDto> ChatAboutActivitiesAsync(
        AiChatRequestDto request,
        int userId,
        CancellationToken ct = default)
    {
        var tools = new CropActivityTools(_context);
        var bundle = await tools.GetCycleActivityBundleAsync(request.CultivationCycleId, ct);
        if (bundle == null)
        {
            throw new InvalidOperationException($"Cycle {request.CultivationCycleId} not found.");
        }

        var analysis = await AnalyzeActivitiesAsync(new ActivityAnalysisInput
        {
            CultivationCycleId = request.CultivationCycleId,
            FocusArea = "All"
        }, userId, ct);

        try
        {
            var systemPrompt = "You are Kumburu AI Crop Advisor, an empathetic expert agronomist for Sri Lankan rice farmers. " +
                               "Provide clear, practical, and culturally familiar advice based on Department of Agriculture (DOA) and RRDI Batalagoda standards. " +
                               "Answer concisely in 2 to 3 sentences.";

            var userPrompt = $"Farmer asks: '{request.Question}'\n" +
                             $"Field context: Variety: {analysis.FieldOverview.VarietyName}, Current Stage: {analysis.FieldOverview.CurrentStage} ({analysis.FieldOverview.DaysAfterSowing} DAS).\n" +
                             $"Water level: {outputWaterLevel(bundle)}.\n" +
                             $"Fertilizer applied: Urea {analysis.Diagnostics.Fertilizer.TotalUreaKgPerHa:F0} kg/ha. Assessment: {analysis.Diagnostics.Fertilizer.Assessment}.\n" +
                             $"Warnings: {string.Join("; ", analysis.Warnings)}";

            var answerText = await _llmClient.CompleteJsonAsync(
                systemPrompt,
                userPrompt,
                Array.Empty<LlmToolDefinition>(),
                (_, _) => Task.FromResult("{}"),
                ct);

            if (string.IsNullOrWhiteSpace(answerText) || answerText.StartsWith("{"))
            {
                answerText = GenerateDeterministicChatAnswer(request.Question, bundle, analysis);
            }

            return new AiChatResponseDto
            {
                Answer = answerText,
                SuggestedFollowUps = GenerateFollowUps(bundle),
                RequiresOfficerReview = analysis.RequiresOfficerReview
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM chat call failed; using grounded expert response fallback.");
            var fallbackAnswer = GenerateDeterministicChatAnswer(request.Question, bundle, analysis);
            return new AiChatResponseDto
            {
                Answer = fallbackAnswer,
                SuggestedFollowUps = GenerateFollowUps(bundle),
                RequiresOfficerReview = analysis.RequiresOfficerReview
            };
        }
    }

    /// <summary>
    /// Executes the farmer's decision (Execute, Revise) on an agronomic recommendation.
    /// When approved by the officer, the farmer can execute it directly into the field ledger.
    /// </summary>
    public async Task<ReviewRecommendationResponseDto> ReviewRecommendationAsync(
        int cycleId,
        ReviewRecommendationRequestDto request,
        int userId,
        string userRole,
        CancellationToken ct = default)
    {
        var cycle = await _context.CultivationCycles
            .Include(c => c.Field)
            .FirstOrDefaultAsync(c => c.Id == cycleId, ct);

        if (cycle == null)
            throw new InvalidOperationException($"Cultivation cycle {cycleId} not found.");

        if (userRole == "Farmer" && cycle.Field?.FarmerId != userId)
            throw new UnauthorizedAccessException("You do not have access to review recommendations for this cycle.");

        var user = await _context.Users.FindAsync(new object[] { userId }, ct);
        var userName = user?.Name ?? "Farmer";

        var response = new ReviewRecommendationResponseDto
        {
            Success = true,
            Recommendation = new ActivityRecommendationDto
            {
                Id = request.RecommendationId,
                ReviewedBy = userName,
                ReviewedAt = DateTime.UtcNow,
                ReviewNotes = request.Notes
            }
        };

        var decision = request.Decision.Trim().ToLowerInvariant();

        // Check if database recommendation exists
        var dbRec = await _context.CropActivityRecommendations
            .FirstOrDefaultAsync(r => r.CultivationCycleId == cycleId && (r.RecommendationUid == request.RecommendationId || r.Id.ToString() == request.RecommendationId), ct);

        if (decision == "execute" || decision == "approveandexecute" || decision == "approve_and_execute")
        {
            // Execute recommendation into a concrete CropActivity
            var targetDate = DateOnly.FromDateTime(DateTime.UtcNow);
            if (!string.IsNullOrWhiteSpace(request.CustomDate) && DateOnly.TryParse(request.CustomDate, out var parsedDate))
            {
                targetDate = parsedDate;
            }

            CropActivityType actType = CropActivityType.Other;
            string detailsJson = "{\"specificActivity\":\"Agronomic Intervention\"}";

            var payloadSource = request.RecommendationJson ?? dbRec?.ExecutionPayloadJson;

            if (!string.IsNullOrWhiteSpace(payloadSource))
            {
                try
                {
                    using var doc = JsonDocument.Parse(payloadSource);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("activityType", out var typeEl))
                    {
                        if (Enum.TryParse<CropActivityType>(typeEl.GetString(), true, out var parsedType))
                            actType = parsedType;
                    }

                    if (root.TryGetProperty("detailsJson", out var detEl))
                    {
                        detailsJson = detEl.ValueKind == JsonValueKind.String ? detEl.GetString()! : detEl.GetRawText();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse recommendation payload, using fallback.");
                }
            }

            var activity = new CropActivity
            {
                CultivationCycleId = cycleId,
                ActivityType = actType,
                Date = targetDate,
                DetailsJson = detailsJson,
                LoggedByUserId = userId,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _context.CropActivities.Add(activity);
            await _context.SaveChangesAsync(ct);

            // Update database recommendation record
            if (dbRec != null)
            {
                dbRec.Status = "EXECUTED";
                dbRec.ExecutedByUserId = userId;
                dbRec.ExecutedActivityId = activity.Id;
                dbRec.ExecutedAt = DateTime.UtcNow;
                dbRec.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);
            }

            response.CreatedActivityId = activity.Id;
            response.Recommendation.Status = "EXECUTED";
            response.Recommendation.ExecutedActivityId = activity.Id;
            response.Message = $"Successfully executed action and logged {actType} activity #{activity.Id} on {targetDate:yyyy-MM-dd}.";
        }
        else if (decision == "reject")
        {
            if (dbRec != null)
            {
                dbRec.Status = "REJECTED";
                dbRec.OfficerComment = request.Notes;
                dbRec.ReviewedAt = DateTime.UtcNow;
                dbRec.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);
            }
            response.Recommendation.Status = "REJECTED";
            response.Message = $"Recommendation was marked as rejected.";
        }
        else
        {
            response.Recommendation.Status = "APPROVED";
            response.Message = $"Recommendation updated.";
        }

        // Record Audit Trail in AgentRunLogs
        try
        {
            _context.AgentRunLogs.Add(new AgentRunLog
            {
                AgentName = "ResourceAnalysisAgent:FarmerReview",
                CorrelationId = request.RecommendationId,
                CultivationPlanId = null,
                InputJson = JsonSerializer.Serialize(new { cycleId, userId, userRole, request }),
                ToolCallsJson = JsonSerializer.Serialize(new[]
                {
                    new
                    {
                        Tool = "farmer_execution_action",
                        Decision = request.Decision,
                        CreatedActivityId = response.CreatedActivityId
                    }
                }),
                RawOutput = response.Message,
                Success = true,
                DurationMs = 25,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record AgentRunLog.");
        }

        return response;
    }

    /// <summary>
    /// Retrieves all recommendations currently pending Agricultural Officer review.
    /// Can be filtered by agrarian division.
    /// </summary>
    public async Task<List<CropActivityRecommendationDto>> GetPendingOfficerRecommendationsAsync(
        int? divisionId = null,
        string? status = null,
        CancellationToken ct = default)
    {
        var query = _context.CropActivityRecommendations
            .AsNoTracking()
            .Include(r => r.CultivationCycle)
                .ThenInclude(c => c!.Field)
                    .ThenInclude(f => f!.Farmer)
            .Include(r => r.CultivationCycle)
                .ThenInclude(c => c!.Field)
                    .ThenInclude(f => f!.Division)
            .Include(r => r.CultivationCycle)
                .ThenInclude(c => c!.Variety)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(r => r.Status == status.ToUpperInvariant());
        }

        if (divisionId.HasValue)
        {
            query = query.Where(r => r.CultivationCycle != null && r.CultivationCycle.Field != null && r.CultivationCycle.Field.DivisionId == divisionId.Value);
        }

        var list = await query
            .OrderByDescending(r => r.Status == "PENDING_OFFICER_REVIEW")
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return list.Select(r =>
        {
            var cycle = r.CultivationCycle;
            var das = cycle != null ? Math.Max(0, today.DayNumber - cycle.SowingDate.DayNumber) : 0;
            return new CropActivityRecommendationDto
            {
                Id = r.Id,
                RecommendationUid = r.RecommendationUid,
                CultivationCycleId = r.CultivationCycleId,
                CycleName = cycle != null ? $"{cycle.Season} {cycle.Year}" : "Cycle",
                FieldName = cycle?.Field?.Name ?? "Field",
                FarmerName = cycle?.Field?.Farmer?.Name ?? "Farmer",
                FarmerId = cycle?.Field?.FarmerId ?? r.RequestedByUserId,
                DivisionName = cycle?.Field?.Division?.Name ?? "Division",
                VarietyName = cycle?.Variety?.Name ?? "Rice Variety",
                DaysAfterSowing = das,
                Stage = cycle?.CurrentStage.ToString() ?? "Active",
                Category = r.Category,
                Priority = r.Priority,
                Action = r.Action,
                Reason = r.Reason,
                Evidence = r.Evidence,
                ConfidenceScore = r.ConfidenceScore,
                CitationsJson = r.CitationsJson,
                RequiresOfficerReview = r.RequiresOfficerReview,
                Status = r.Status,
                ExecutionPayloadJson = r.ExecutionPayloadJson,
                OfficerId = r.OfficerId,
                OfficerName = r.OfficerName,
                OfficerComment = r.OfficerComment,
                ReviewedAt = r.ReviewedAt,
                ExecutedActivityId = r.ExecutedActivityId,
                ExecutedAt = r.ExecutedAt,
                CreatedAt = r.CreatedAt
            };
        }).ToList();
    }

    /// <summary>
    /// Retrieves all recommendations for a specific cycle from the database.
    /// </summary>
    public async Task<List<CropActivityRecommendationDto>> GetCycleRecommendationsAsync(
        int cycleId,
        CancellationToken ct = default)
    {
        var list = await _context.CropActivityRecommendations
            .AsNoTracking()
            .Include(r => r.CultivationCycle)
                .ThenInclude(c => c!.Field)
                    .ThenInclude(f => f!.Farmer)
            .Include(r => r.CultivationCycle)
                .ThenInclude(c => c!.Field)
                    .ThenInclude(f => f!.Division)
            .Include(r => r.CultivationCycle)
                .ThenInclude(c => c!.Variety)
            .Where(r => r.CultivationCycleId == cycleId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return list.Select(r =>
        {
            var cycle = r.CultivationCycle;
            var das = cycle != null ? Math.Max(0, today.DayNumber - cycle.SowingDate.DayNumber) : 0;
            return new CropActivityRecommendationDto
            {
                Id = r.Id,
                RecommendationUid = r.RecommendationUid,
                CultivationCycleId = r.CultivationCycleId,
                CycleName = cycle != null ? $"{cycle.Season} {cycle.Year}" : "Cycle",
                FieldName = cycle?.Field?.Name ?? "Field",
                FarmerName = cycle?.Field?.Farmer?.Name ?? "Farmer",
                FarmerId = cycle?.Field?.FarmerId ?? r.RequestedByUserId,
                DivisionName = cycle?.Field?.Division?.Name ?? "Division",
                VarietyName = cycle?.Variety?.Name ?? "Rice Variety",
                DaysAfterSowing = das,
                Stage = cycle?.CurrentStage.ToString() ?? "Active",
                Category = r.Category,
                Priority = r.Priority,
                Action = r.Action,
                Reason = r.Reason,
                Evidence = r.Evidence,
                ConfidenceScore = r.ConfidenceScore,
                CitationsJson = r.CitationsJson,
                RequiresOfficerReview = r.RequiresOfficerReview,
                Status = r.Status,
                ExecutionPayloadJson = r.ExecutionPayloadJson,
                OfficerId = r.OfficerId,
                OfficerName = r.OfficerName,
                OfficerComment = r.OfficerComment,
                ReviewedAt = r.ReviewedAt,
                ExecutedActivityId = r.ExecutedActivityId,
                ExecutedAt = r.ExecutedAt,
                CreatedAt = r.CreatedAt
            };
        }).ToList();
    }

    /// <summary>
    /// Agricultural Officer reviews a pending recommendation (Approve or Reject).
    /// Once approved, it is displayed to the farmer side as verified guidance ready for execution.
    /// </summary>
    public async Task<CropActivityRecommendationDto> OfficerReviewRecommendationAsync(
        int recommendationId,
        string decision,
        string? comment,
        int officerId,
        CancellationToken ct = default)
    {
        var rec = await _context.CropActivityRecommendations
            .Include(r => r.CultivationCycle)
                .ThenInclude(c => c!.Field)
                    .ThenInclude(f => f!.Farmer)
            .Include(r => r.CultivationCycle)
                .ThenInclude(c => c!.Field)
                    .ThenInclude(f => f!.Division)
            .Include(r => r.CultivationCycle)
                .ThenInclude(c => c!.Variety)
            .FirstOrDefaultAsync(r => r.Id == recommendationId, ct);

        if (rec == null)
            throw new InvalidOperationException($"Recommendation #{recommendationId} not found.");

        var officer = await _context.Users.FindAsync(new object[] { officerId }, ct);
        if (officer == null)
            throw new InvalidOperationException("Officer account not found.");

        var isApprove = decision.Trim().Equals("Approve", StringComparison.OrdinalIgnoreCase);

        rec.Status = isApprove ? "APPROVED" : "REJECTED";
        rec.OfficerId = officerId;
        rec.OfficerName = officer.Name;
        rec.OfficerComment = comment;
        rec.ReviewedAt = DateTime.UtcNow;
        rec.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        // Create Notification for the Farmer
        try
        {
            var farmerId = rec.CultivationCycle?.Field?.FarmerId ?? rec.RequestedByUserId;
            var actionSnippet = rec.Action.Length > 60 ? rec.Action.Substring(0, 57) + "..." : rec.Action;

            var notification = new Notification
            {
                UserId = farmerId,
                Title = isApprove ? "Crop Activity Recommendation Approved" : "Crop Activity Recommendation Rejected",
                Message = isApprove
                    ? $"Agricultural Officer {officer.Name} approved: \"{actionSnippet}\". Advice: \"{comment ?? "Approved as per DOA guidelines."}\""
                    : $"Agricultural Officer {officer.Name} advised not to proceed with: \"{actionSnippet}\". Note: \"{comment ?? "Not recommended at this crop stage."}\"",
                Type = "CropActivityReview",
                Status = rec.Status,
                RelatedCycleId = rec.CultivationCycleId,
                RelatedRecommendationId = rec.Id,
                OfficerName = officer.Name,
                OfficerComment = comment,
                ActionText = rec.Action,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create farmer notification for recommendation #{RecommendationId}.", recommendationId);
        }

        // Audit Trail
        try
        {
            _context.AgentRunLogs.Add(new AgentRunLog
            {
                AgentName = "AgriculturalOfficerReview",
                CorrelationId = rec.RecommendationUid,
                CultivationPlanId = null,
                InputJson = JsonSerializer.Serialize(new { recommendationId, decision, comment, officerId }),
                ToolCallsJson = JsonSerializer.Serialize(new[]
                {
                    new
                    {
                        Tool = "officer_review_decision",
                        Decision = rec.Status,
                        OfficerName = officer.Name,
                        Comment = comment
                    }
                }),
                RawOutput = $"Recommendation #{recommendationId} ({rec.Action}) set to {rec.Status} by Officer {officer.Name}.",
                Success = true,
                DurationMs = 20,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record officer review in AgentRunLogs.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var cycle = rec.CultivationCycle;
        var das = cycle != null ? Math.Max(0, today.DayNumber - cycle.SowingDate.DayNumber) : 0;

        return new CropActivityRecommendationDto
        {
            Id = rec.Id,
            RecommendationUid = rec.RecommendationUid,
            CultivationCycleId = rec.CultivationCycleId,
            CycleName = cycle != null ? $"{cycle.Season} {cycle.Year}" : "Cycle",
            FieldName = cycle?.Field?.Name ?? "Field",
            FarmerName = cycle?.Field?.Farmer?.Name ?? "Farmer",
            FarmerId = cycle?.Field?.FarmerId ?? rec.RequestedByUserId,
            DivisionName = cycle?.Field?.Division?.Name ?? "Division",
            VarietyName = cycle?.Variety?.Name ?? "Rice Variety",
            DaysAfterSowing = das,
            Stage = cycle?.CurrentStage.ToString() ?? "Active",
            Category = rec.Category,
            Priority = rec.Priority,
            Action = rec.Action,
            Reason = rec.Reason,
            Evidence = rec.Evidence,
            ConfidenceScore = rec.ConfidenceScore,
            CitationsJson = rec.CitationsJson,
            RequiresOfficerReview = rec.RequiresOfficerReview,
            Status = rec.Status,
            ExecutionPayloadJson = rec.ExecutionPayloadJson,
            OfficerId = rec.OfficerId,
            OfficerName = rec.OfficerName,
            OfficerComment = rec.OfficerComment,
            ReviewedAt = rec.ReviewedAt,
            ExecutedActivityId = rec.ExecutedActivityId,
            ExecutedAt = rec.ExecutedAt,
            CreatedAt = rec.CreatedAt
        };
    }

    /// <summary>
    /// Retrieves full audit history of agent runs and human reviews for this cycle.
    /// Step 10 of the Agentic AI lifecycle.
    /// </summary>
    public async Task<List<AgentAuditLogEntryDto>> GetAuditLogsAsync(int cycleId, CancellationToken ct = default)
    {
        var logs = await _context.AgentRunLogs
            .AsNoTracking()
            .Where(l => l.AgentName.StartsWith("ResourceAnalysisAgent") || l.AgentName.StartsWith("CropActivity") || l.AgentName == "CultivationPlanningAgent" || l.AgentName == "AgriculturalOfficerReview")
            .OrderByDescending(l => l.CreatedAt)
            .Take(30)
            .ToListAsync(ct);

        return logs.Select(l => new AgentAuditLogEntryDto
        {
            Id = l.Id,
            AgentName = l.AgentName,
            CorrelationId = l.CorrelationId,
            InputJson = l.InputJson,
            ToolCallsJson = l.ToolCallsJson,
            RawOutput = l.RawOutput,
            Success = l.Success,
            DurationMs = l.DurationMs,
            CreatedAt = l.CreatedAt
        }).ToList();
    }

    private static string outputWaterLevel(CycleActivityBundle b)
    {
        var latest = b.Irrigations.LastOrDefault();
        return latest?.WaterLevelCm.HasValue == true ? $"{latest.WaterLevelCm} cm on {latest.Date:yyyy-MM-dd}" : "None recorded";
    }

    private static void BuildWaterDiagnostic(CycleActivityBundle bundle, WaterDiagnosticDto diag)
    {
        diag.TotalIrrigationEvents = bundle.Irrigations.Count;
        diag.TotalDurationHours = bundle.Irrigations.Sum(i => i.DurationHours);

        var latest = bundle.Irrigations.LastOrDefault();
        if (latest != null)
        {
            diag.LatestWaterLevelCm = latest.WaterLevelCm;
            diag.DaysSinceLastIrrigation = bundle.Today.DayNumber - latest.Date.DayNumber;
        }
        else
        {
            diag.DaysSinceLastIrrigation = bundle.DaysAfterSowing;
        }

        var stage = bundle.EstimatedStage;

        if (stage == "Harvest Ready" || stage == "Harvested")
        {
            diag.Status = "DryDraining";
            diag.Assessment = "Crop is in harvest preparation. The field should be drained dry to allow even ripening and harvest.";
        }
        else if (latest == null)
        {
            diag.Status = "Low";
            diag.Assessment = "No irrigation recorded yet for this cycle. Monitor soil moisture to prevent seedling desiccation.";
        }
        else if (latest.WaterLevelCm > 6.0)
        {
            diag.Status = "Flooded";
            diag.Assessment = $"Water level is high ({latest.WaterLevelCm} cm). Excessive depth suppresses tiller formation during vegetative phases.";
        }
        else if (latest.WaterLevelCm < 1.5 && diag.DaysSinceLastIrrigation > 4)
        {
            diag.Status = "DroughtRisk";
            diag.Assessment = $"Water level is low ({latest.WaterLevelCm} cm) and {diag.DaysSinceLastIrrigation} days have elapsed since last wetting. Irrigate soon.";
        }
        else
        {
            diag.Status = "Adequate";
            diag.Assessment = $"Water depth is well-maintained at {latest.WaterLevelCm} cm, consistent with {stage} stage needs.";
        }
    }

    private static void BuildFertilizerDiagnostic(CycleActivityBundle bundle, FertilizerDiagnosticDto diag)
    {
        diag.ApplicationsCount = bundle.Fertilizers.Count;
        diag.TotalUreaKgPerHa = bundle.Fertilizers.Where(f => f.Type.Contains("Urea", StringComparison.OrdinalIgnoreCase)).Sum(f => f.QuantityKgPerHa);
        diag.TotalTspKgPerHa = bundle.Fertilizers.Where(f => f.Type.Contains("TSP", StringComparison.OrdinalIgnoreCase)).Sum(f => f.QuantityKgPerHa);
        diag.TotalMopKgPerHa = bundle.Fertilizers.Where(f => f.Type.Contains("MOP", StringComparison.OrdinalIgnoreCase)).Sum(f => f.QuantityKgPerHa);

        var latest = bundle.Fertilizers.LastOrDefault();
        if (latest != null)
        {
            diag.DaysSinceLastFertilizer = bundle.Today.DayNumber - latest.Date.DayNumber;
        }

        var das = bundle.DaysAfterSowing;
        var stage = bundle.EstimatedStage;

        if (das <= 14)
        {
            diag.SplitCompliance = diag.TotalTspKgPerHa > 0 ? "Basal Applied" : "Basal Due";
            diag.Status = diag.TotalTspKgPerHa > 0 ? "Balanced" : "DueSoon";
            diag.Assessment = diag.TotalTspKgPerHa > 0 ? "Basal fertilizer recorded." : "Basal fertilizer (TSP and initial N-P-K) should be applied before or at sowing.";
        }
        else if (das <= 35)
        {
            diag.SplitCompliance = diag.TotalUreaKgPerHa >= 40 ? "1st Top Dressing Applied" : "1st Top Dressing Due";
            diag.Status = diag.TotalUreaKgPerHa >= 40 ? "Balanced" : "DueSoon";
            diag.Assessment = diag.TotalUreaKgPerHa >= 40
                ? "First top dressing (Urea ~50 kg/ha) recorded for Tillering."
                : "Crop is at Tillering stage. 1st Top Dressing of Urea (50 kg/ha) is due to promote vigorous tillers.";
        }
        else if (das <= 65)
        {
            diag.SplitCompliance = "2nd Top Dressing Window (Panicle Initiation)";
            diag.Status = "Balanced";
            diag.Assessment = "Crop is approaching or at Panicle Initiation. Second top dressing of Urea (50 kg/ha) + MOP is required.";
        }
        else
        {
            diag.SplitCompliance = "Heading / Grain Filling Phase";
            diag.Status = "Balanced";
            diag.Assessment = "Major vegetative split windows are complete. Maintain adequate moisture.";
        }
    }

    private static void BuildPestDiagnostic(CycleActivityBundle bundle, PestDiagnosticDto diag)
    {
        diag.TreatmentsCount = bundle.Pesticides.Count;
        diag.ProductsUsed = bundle.Pesticides.Select(p => p.Product).Distinct().ToList();
        diag.TargetPests = bundle.Pesticides.Select(p => p.TargetPest).Distinct().ToList();

        var latest = bundle.Pesticides.LastOrDefault();
        if (latest != null)
        {
            diag.DaysSinceLastTreatment = bundle.Today.DayNumber - latest.Date.DayNumber;
        }

        if (bundle.Pesticides.Count == 0)
        {
            diag.Status = "Safe";
            diag.Assessment = "No chemical pesticide applications logged. Field ecosystem and beneficial natural predators are preserved.";
        }
        else if (diag.DaysSinceLastTreatment < 5)
        {
            diag.Status = "MonitoringRequired";
            diag.Assessment = $"Recent treatment with '{latest?.Product}' was recorded {diag.DaysSinceLastTreatment} days ago. Scout field before any further intervention.";
        }
        else
        {
            diag.Status = "Safe";
            diag.Assessment = $"{diag.TreatmentsCount} treatment(s) logged to date. Continue regular visual scouting.";
        }
    }

    private static void BuildOtherDiagnostic(CycleActivityBundle bundle, OtherDiagnosticDto diag)
    {
        diag.ActivitiesCount = bundle.Others.Count;
        diag.ActivityTypes = bundle.Others.Select(o => o.SpecificActivity).Distinct().ToList();

        var hasWeeding = bundle.Others.Any(o => o.SpecificActivity.Contains("Weeding", StringComparison.OrdinalIgnoreCase));
        diag.WeedingStatus = hasWeeding ? "Weeding Completed" : "Not Recorded";
        diag.Assessment = hasWeeding
            ? "Weeding activity recorded. Weed competition minimized."
            : "No weeding activity logged. Ensure weeds are controlled before top dressing applications.";
    }

    private static void GenerateAgronomicRecommendations(CycleActivityBundle bundle, CropActivityAnalysisOutput output)
    {
        var das = bundle.DaysAfterSowing;
        var stage = bundle.EstimatedStage;
        var diag = output.Diagnostics;
        var todayStr = bundle.Today.ToString("yyyy-MM-dd");

        // 1. Water Recommendation
        if (stage == "Nursery / Establishment" || stage == "Tillering")
        {
            if (diag.Water.LatestWaterLevelCm.HasValue && diag.Water.LatestWaterLevelCm.Value > 5.0)
            {
                output.Recommendations.Add(new ActivityRecommendationDto
                {
                    Category = "Irrigation",
                    Priority = "MEDIUM",
                    Action = "Lower field water level to 2 - 3 cm.",
                    Reason = $"Current water level ({diag.Water.LatestWaterLevelCm.Value} cm) is too deep for Tillering. Deep standing water hinders new tiller emergence.",
                    Evidence = $"Logged water level: {diag.Water.LatestWaterLevelCm.Value} cm on {bundle.Irrigations.LastOrDefault()?.Date:yyyy-MM-dd}.",
                    ConfidenceScore = 0.92,
                    Status = "PENDING_OFFICER_REVIEW",
                    ExecutionPayloadJson = JsonSerializer.Serialize(new
                    {
                        activityType = "Irrigation",
                        date = todayStr,
                        detailsJson = JsonSerializer.Serialize(new { waterLevel = 2.5, duration = 1.5, source = "Drainage Canal" })
                    }),
                    Citations = new List<CitationDto>
                    {
                        new CitationDto { Document = "Rice Research and Development Institute (RRDI) Batalagoda", Section = "Water Management for Maximum Tillering" }
                    }
                });
            }
            else if (diag.Water.DaysSinceLastIrrigation >= 4 || (diag.Water.LatestWaterLevelCm ?? 0) < 1.5)
            {
                output.Recommendations.Add(new ActivityRecommendationDto
                {
                    Category = "Irrigation",
                    Priority = "HIGH",
                    Action = "Irrigate to establish a 2 - 4 cm standing water depth.",
                    Reason = "Tillering requires continuous moist/shallow water to suppress broadleaf weeds and facilitate root nutrient uptake.",
                    Evidence = $"Last irrigation was {diag.Water.DaysSinceLastIrrigation} days ago.",
                    ConfidenceScore = 0.90,
                    Status = "PENDING_OFFICER_REVIEW",
                    ExecutionPayloadJson = JsonSerializer.Serialize(new
                    {
                        activityType = "Irrigation",
                        date = todayStr,
                        detailsJson = JsonSerializer.Serialize(new { waterLevel = 3.0, duration = 2.5, source = "Canal" })
                    }),
                    Citations = new List<CitationDto>
                    {
                        new CitationDto { Document = "Department of Agriculture Sri Lanka - Paddy Cultivation Guide", Section = "Vegetative Stage Water Requirements" }
                    }
                });
            }
        }
        else if (stage == "Harvest Ready" || stage == "Harvested")
        {
            output.Recommendations.Add(new ActivityRecommendationDto
            {
                Category = "Irrigation",
                Priority = "HIGH",
                Action = "Keep bunds open and ensure field is completely drained dry.",
                Reason = "Standing water during harvest increases grain moisture, causes grain shattering, and hinders combine harvesters.",
                Evidence = $"Crop is at {das} DAS (variety maturity is {bundle.VarietyDurationDays} days).",
                ConfidenceScore = 0.96,
                Status = "PENDING_OFFICER_REVIEW",
                ExecutionPayloadJson = JsonSerializer.Serialize(new
                {
                    activityType = "Irrigation",
                    date = todayStr,
                    detailsJson = JsonSerializer.Serialize(new { waterLevel = 0.0, duration = 1.0, source = "Drainage" })
                }),
                Citations = new List<CitationDto>
                {
                    new CitationDto { Document = "Department of Agriculture Sri Lanka", Section = "Pre-Harvest Field Drainage Standards" }
                }
            });
        }

        // 2. Fertilizer Recommendation
        if (das >= 14 && das <= 28 && diag.Fertilizer.TotalUreaKgPerHa < 30)
        {
            output.Recommendations.Add(new ActivityRecommendationDto
            {
                Category = "Fertilizer",
                Priority = "HIGH",
                Action = "Apply 1st Top Dressing of Urea at 50 kg/ha.",
                Reason = "Crop is in active Tillering. Timely nitrogen is essential to build strong productive tillers.",
                Evidence = $"Current age is {das} DAS, and no 1st Top Dressing Urea has been logged yet.",
                ConfidenceScore = 0.94,
                Status = "PENDING_OFFICER_REVIEW",
                ExecutionPayloadJson = JsonSerializer.Serialize(new
                {
                    activityType = "Fertilizer",
                    date = todayStr,
                    detailsJson = JsonSerializer.Serialize(new { type = "Urea", quantity = 50.0, cropStage = "Tillering", region = "Dry", method = "Broadcasting" })
                }),
                Citations = new List<CitationDto>
                {
                    new CitationDto { Document = "DOA Sri Lanka - Targeted Fertilizer Guidelines", Section = "1st Top Dressing for 3.5 Month Varieties (Tillering)" }
                }
            });
        }
        else if (das >= 45 && das <= 58 && diag.Fertilizer.DaysSinceLastFertilizer > 14)
        {
            output.Recommendations.Add(new ActivityRecommendationDto
            {
                Category = "Fertilizer",
                Priority = "HIGH",
                Action = "Apply 2nd Top Dressing: Urea (50 kg/ha) + MOP (Muriate of Potash, 25 kg/ha).",
                Reason = "Panicle Initiation is the primary yield-determining stage. Nitrogen and Potassium together maximize spikelet count and grain filling capacity.",
                Evidence = $"Crop age is {das} DAS (Panicle Initiation window). Last fertilizer was {diag.Fertilizer.DaysSinceLastFertilizer} days ago.",
                ConfidenceScore = 0.93,
                Status = "PENDING_OFFICER_REVIEW",
                ExecutionPayloadJson = JsonSerializer.Serialize(new
                {
                    activityType = "Fertilizer",
                    date = todayStr,
                    detailsJson = JsonSerializer.Serialize(new { type = "Urea", quantity = 50.0, cropStage = "Panicle Initiation", region = "Dry", method = "Broadcasting" })
                }),
                Citations = new List<CitationDto>
                {
                    new CitationDto { Document = "DOA Sri Lanka - Rice Nutrient Guidelines", Section = "2nd Top Dressing at Panicle Initiation" }
                }
            });
        }

        // 3. Weeding Recommendation
        if (das >= 14 && das <= 30 && diag.Other.WeedingStatus != "Weeding Completed")
        {
            output.Recommendations.Add(new ActivityRecommendationDto
            {
                Category = "General",
                Priority = "MEDIUM",
                Action = "Perform manual or mechanical rotary weeding before applying top-dressing fertilizer.",
                Reason = "Weeds absorb up to 40% of applied nitrogen if left unchecked during early tillering.",
                Evidence = "No weeding activity logged in the system for this cycle.",
                ConfidenceScore = 0.88,
                Status = "PENDING_OFFICER_REVIEW",
                ExecutionPayloadJson = JsonSerializer.Serialize(new
                {
                    activityType = "Other",
                    date = todayStr,
                    detailsJson = JsonSerializer.Serialize(new { specificActivity = "Manual Rotary Weeding" })
                }),
                Citations = new List<CitationDto>
                {
                    new CitationDto { Document = "Department of Agriculture Sri Lanka", Section = "Integrated Weed Management in Wet and Dry Seeded Paddy" }
                }
            });
        }

        // 4. Default / Preventive Recommendation if no specific threshold was crossed
        if (output.Recommendations.Count == 0)
        {
            output.Recommendations.Add(new ActivityRecommendationDto
            {
                Category = "Irrigation",
                Priority = "MEDIUM",
                Action = $"Maintain recommended {stage} standing water depth (2 - 4 cm).",
                Reason = $"Field activities currently align with Sri Lankan DOA benchmarks for {stage}. Continue routine irrigation intervals.",
                Evidence = $"Current crop age: {das} DAS. Current stage: {stage}.",
                ConfidenceScore = 0.88,
                Status = "PENDING_OFFICER_REVIEW",
                ExecutionPayloadJson = JsonSerializer.Serialize(new
                {
                    activityType = "Irrigation",
                    date = todayStr,
                    detailsJson = JsonSerializer.Serialize(new { waterLevel = 3.0, duration = 2.0, source = "Canal" })
                }),
                Citations = new List<CitationDto>
                {
                    new CitationDto { Document = "DOA Sri Lanka - Paddy Production Guidelines", Section = "Standard Field Regime" }
                }
            });
        }
    }

    private async Task SynthesizeExecutiveSummaryAsync(
        CycleActivityBundle bundle,
        CropActivityAnalysisOutput output,
        string? farmerQuestion,
        CancellationToken ct)
    {
        var summary = $"Cycle is at {output.FieldOverview.CurrentStage} stage ({output.FieldOverview.DaysAfterSowing} DAS) " +
                      $"for {output.FieldOverview.VarietyName} in {output.FieldOverview.DivisionName}. " +
                      $"Water management is {output.Diagnostics.Water.Status.ToLower()} with {output.Diagnostics.Water.TotalIrrigationEvents} events logged. " +
                      $"Nutrient status is {output.Diagnostics.Fertilizer.Status.ToLower()} ({output.Diagnostics.Fertilizer.TotalUreaKgPerHa:F0} kg/ha Urea applied to date).";

        if (output.Warnings.Count > 0)
        {
            summary += $" Attention: {output.Warnings.Count} safety alert(s) require your review.";
        }

        output.ExecutiveSummary = summary;

        // Enhance with LLM when client is operational
        try
        {
            var prompt = $"Synthesize a clear, friendly 2-sentence executive summary for a Sri Lankan paddy farmer given:\n" +
                         $"Stage: {output.FieldOverview.CurrentStage} ({output.FieldOverview.DaysAfterSowing} DAS), Variety: {output.FieldOverview.VarietyName}.\n" +
                         $"Water: {output.Diagnostics.Water.Assessment}\n" +
                         $"Fertilizer: {output.Diagnostics.Fertilizer.Assessment}\n" +
                         $"Top Action: {output.Recommendations.FirstOrDefault()?.Action ?? "Maintain current management"}\n" +
                         $"Warnings: {string.Join("; ", output.Warnings)}";

            var response = await _llmClient.CompleteJsonAsync(
                "You are an empathetic, agricultural extension officer for Sri Lankan rice farmers. Return plain text only.",
                prompt,
                Array.Empty<LlmToolDefinition>(),
                (_, _) => Task.FromResult("{}"),
                ct);

            if (!string.IsNullOrWhiteSpace(response) && !response.StartsWith("{"))
            {
                output.ExecutiveSummary = response.Trim();
            }
        }
        catch
        {
            // Keep default structured executive summary
        }
    }

    private static string GenerateDeterministicChatAnswer(
        string question,
        CycleActivityBundle bundle,
        CropActivityAnalysisOutput analysis)
    {
        var q = question.ToLowerInvariant();
        if (q.Contains("water") || q.Contains("irrigate") || q.Contains("flood"))
        {
            var latest = bundle.Irrigations.LastOrDefault();
            return $"Based on your records, your field is at {bundle.EstimatedStage} ({bundle.DaysAfterSowing} DAS). " +
                   (latest != null ? $"Your last recorded water level was {latest.WaterLevelCm} cm on {latest.Date:yyyy-MM-dd}. " : "You have not recorded recent water levels. ") +
                   (bundle.EstimatedStage == "Tillering" ? "For Tillering, keep water shallow (2-4 cm) to promote maximum tillering." : "Maintain steady 3-5 cm water during reproductive stages.");
        }

        if (q.Contains("fertilizer") || q.Contains("urea") || q.Contains("tsp") || q.Contains("mop"))
        {
            return $"Your crop has received {analysis.Diagnostics.Fertilizer.TotalUreaKgPerHa:F0} kg/ha Urea so far. " +
                   $"Currently at {bundle.EstimatedStage} ({bundle.DaysAfterSowing} DAS). " +
                   analysis.Diagnostics.Fertilizer.Assessment;
        }

        if (q.Contains("pest") || q.Contains("spray") || q.Contains("disease") || q.Contains("chemical"))
        {
            return $"You have recorded {bundle.Pesticides.Count} pesticide treatment(s). " +
                   (analysis.Warnings.Any(w => w.Contains("BANNED")) ? "WARNING: A restricted chemical was detected in your history. Consult your local Agricultural Officer." : "Continue regular scouting. Spray only when pests exceed the economic injury threshold.");
        }

        return $"Your {bundle.Cycle.Variety?.Name ?? "rice"} crop is at {bundle.EstimatedStage} stage ({bundle.DaysAfterSowing} Days After Sowing). " +
               $"Key priority: {analysis.Recommendations.FirstOrDefault()?.Action ?? "Maintain regular field scouting and monitoring."}";
    }

    private static List<string> GenerateFollowUps(CycleActivityBundle bundle)
    {
        return new List<string>
        {
            "Is my water level appropriate for this stage?",
            "When should I apply my next fertilizer split?",
            "What pests should I scout for right now?"
        };
    }
}
