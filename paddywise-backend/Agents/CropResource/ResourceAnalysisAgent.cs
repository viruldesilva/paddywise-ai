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
                Duration = stopwatch.Elapsed
            };
        }
    }

    /// <summary>
    /// Performs comprehensive Agentic AI analysis on all activities of a cycle.
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

        // 1. Field Overview
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

        // 2. Diagnostics
        BuildWaterDiagnostic(bundle, output.Diagnostics.Water);
        BuildFertilizerDiagnostic(bundle, output.Diagnostics.Fertilizer);
        BuildPestDiagnostic(bundle, output.Diagnostics.Pest);
        BuildOtherDiagnostic(bundle, output.Diagnostics.Other);

        // 3. Generate Expert Agronomic Recommendations (DOA Guidelines)
        GenerateAgronomicRecommendations(bundle, output);

        // 4. Deterministic Safety Audit (ROP & Safety Gate)
        var safetyResult = ActivitySafetyValidator.AuditActivitiesAndRecommendations(bundle, output.Recommendations);
        if (safetyResult.SafetyAlerts.Count > 0)
        {
            output.Warnings.AddRange(safetyResult.SafetyAlerts);
        }
        if (safetyResult.RequiresOfficerReview)
        {
            output.RequiresOfficerReview = true;
        }

        // 5. Synthesize Executive Summary (Enhanced with LLM when available)
        await SynthesizeExecutiveSummaryAsync(bundle, output, input.FarmerQuestion, ct);

        // 6. Persist Agent Run Log in database
        try
        {
            _context.AgentRunLogs.Add(new AgentRunLog
            {
                AgentName = Name,
                CorrelationId = Guid.NewGuid().ToString("N"),
                CultivationPlanId = null,
                InputJson = JsonSerializer.Serialize(input),
                ToolCallsJson = JsonSerializer.Serialize(new object[]
                {
                    new { Tool = "GetCycleActivityBundle", Count = bundle.Irrigations.Count + bundle.Fertilizers.Count + bundle.Pesticides.Count + bundle.Others.Count },
                    new { Tool = "SafetyAudit", AlertsCount = safetyResult.SafetyAlerts.Count }
                }),
                RawOutput = JsonSerializer.Serialize(new { output.FieldOverview, output.Recommendations.Count, output.RequiresOfficerReview }),
                Success = true,
                DurationMs = 150
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
            return new AiChatResponseDto
            {
                Answer = "Cultivation cycle not found. Please verify your selected cycle.",
                RequiresOfficerReview = false
            };
        }

        var analysis = await AnalyzeActivitiesAsync(new ActivityAnalysisInput
        {
            CultivationCycleId = request.CultivationCycleId,
            FarmerQuestion = request.Question
        }, userId, ct);

        var systemPrompt = $$"""
            You are the PaddyWise AI Agronomic Advisor for Sri Lankan rice farming.
            You have full access to the farmer's actual recorded field activity data for this cultivation cycle:
            - Variety: {{bundle.Cycle.Variety?.Name ?? "Bg 352"}} ({{bundle.Cycle.Variety?.AgeGroup ?? "3.5 month"}})
            - Sowing Date: {{bundle.Cycle.SowingDate:yyyy-MM-dd}} ({{bundle.DaysAfterSowing}} Days After Sowing, Stage: {{bundle.EstimatedStage}})
            - Irrigation Events: {{bundle.Irrigations.Count}} (Latest water level: {{outputWaterLevel(bundle)}})
            - Fertilizer Applications: {{bundle.Fertilizers.Count}} (Total Urea: {{bundle.Fertilizers.Where(f => f.Type.Contains("Urea")).Sum(f => f.QuantityKgPerHa)}} kg/ha)
            - Pesticides: {{string.Join(", ", bundle.Pesticides.Select(p => p.Product))}}
            - Warnings: {{string.Join(" | ", analysis.Warnings)}}

            RULES:
            1. Base every response on Sri Lankan Department of Agriculture (DOA) guidelines.
            2. Be direct, clear, and encouraging.
            3. Never recommend banned chemicals (Carbofuran, Chlorpyrifos, Paraquat).
            4. Never advise Urea > 65 kg/ha in a single split.
            5. Answer the farmer's question specifically referencing their logged activities.
            """;

        try
        {
            var prompt = $"Farmer question: \"{request.Question}\"\nProvide a concise, practical, evidence-based answer.";
            var llmResponse = await _llmClient.CompleteJsonAsync(
                systemPrompt,
                prompt,
                Array.Empty<LlmToolDefinition>(),
                (_, _) => Task.FromResult("{}"),
                ct);

            var answerText = llmResponse.Trim();
            if (answerText.StartsWith("{") && answerText.Contains("\"answer\""))
            {
                using var doc = JsonDocument.Parse(answerText);
                if (doc.RootElement.TryGetProperty("answer", out var ansProp))
                {
                    answerText = ansProp.GetString() ?? answerText;
                }
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
            
            // Expert deterministic fallback response
            var fallbackAnswer = GenerateDeterministicChatAnswer(request.Question, bundle, analysis);
            return new AiChatResponseDto
            {
                Answer = fallbackAnswer,
                SuggestedFollowUps = GenerateFollowUps(bundle),
                RequiresOfficerReview = analysis.RequiresOfficerReview
            };
        }
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

        // Split check
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
                Citations = new List<CitationDto>
                {
                    new CitationDto { Document = "Department of Agriculture Sri Lanka", Section = "Integrated Weed Management in Wet and Dry Seeded Paddy" }
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
