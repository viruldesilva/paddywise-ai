using System;
using System.Collections.Generic;

namespace PaddyWise.Api.Agents.CropResource;

public class ActivityAnalysisInput
{
    public int CultivationCycleId { get; set; }
    public string? Objective { get; set; }
    public string? FarmerQuestion { get; set; }
    public string? FocusArea { get; set; } // "All", "Irrigation", "Fertilizer", "Pesticide"
}

public class CropActivityAnalysisOutput
{
    public FieldOverviewDto FieldOverview { get; set; } = new();
    public string UserObjective { get; set; } = string.Empty;
    public DiagnosticsSummaryDto Diagnostics { get; set; } = new();
    public List<ActivityRecommendationDto> Recommendations { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public bool RequiresOfficerReview { get; set; }
    public string ExecutiveSummary { get; set; } = string.Empty;
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

    // 10-Step Pipeline Traces
    public List<DelegatedAgentTaskDto> DelegatedTasks { get; set; } = new();
    public List<ControlledToolCallDto> ControlledToolsInvoked { get; set; } = new();
    public GuardrailValidationReportDto ValidationReport { get; set; } = new();
    public AgentAuditSummaryDto AuditSummary { get; set; } = new();
}

public class DelegatedAgentTaskDto
{
    public string AgentName { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string TaskDescription { get; set; } = string.Empty;
    public string Status { get; set; } = "Completed";
    public string OutputSummary { get; set; } = string.Empty;
}

public class ControlledToolCallDto
{
    public string ToolName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ArgsJson { get; set; } = string.Empty;
    public string ResultSummary { get; set; } = string.Empty;
}

public class GuardrailValidationReportDto
{
    public bool OverdoseCheckPassed { get; set; } = true;
    public bool BannedChemicalCheckPassed { get; set; } = true;
    public bool PreHarvestIntervalCheckPassed { get; set; } = true;
    public bool WaterStressCheckPassed { get; set; } = true;
    public List<string> ChecksDetail { get; set; } = new();
}

public class AgentAuditSummaryDto
{
    public string RunId { get; set; } = Guid.NewGuid().ToString("N");
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
    public int DurationMs { get; set; }
    public string ModelEngine { get; set; } = "PaddyWise-Agentic-v2 (Gemini + DOA Bathalagoda Guardrails)";
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class FieldOverviewDto
{
    public int CycleId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string VarietyName { get; set; } = string.Empty;
    public string AgeGroup { get; set; } = string.Empty; // e.g. "3.5 month"
    public int DaysAfterSowing { get; set; }
    public string CurrentStage { get; set; } = string.Empty;
    public string Season { get; set; } = string.Empty;
    public int Year { get; set; }
    public string DivisionName { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
}

public class DiagnosticsSummaryDto
{
    public WaterDiagnosticDto Water { get; set; } = new();
    public FertilizerDiagnosticDto Fertilizer { get; set; } = new();
    public PestDiagnosticDto Pest { get; set; } = new();
    public OtherDiagnosticDto Other { get; set; } = new();
}

public class WaterDiagnosticDto
{
    public string Status { get; set; } = "Adequate"; // "Adequate", "Low", "Flooded", "DroughtRisk", "DryDraining"
    public double? LatestWaterLevelCm { get; set; }
    public int TotalIrrigationEvents { get; set; }
    public double TotalDurationHours { get; set; }
    public int DaysSinceLastIrrigation { get; set; }
    public string Assessment { get; set; } = string.Empty;
}

public class FertilizerDiagnosticDto
{
    public string Status { get; set; } = "Balanced"; // "Balanced", "Deficient", "Excessive", "DueSoon"
    public double TotalUreaKgPerHa { get; set; }
    public double TotalTspKgPerHa { get; set; }
    public double TotalMopKgPerHa { get; set; }
    public int ApplicationsCount { get; set; }
    public int DaysSinceLastFertilizer { get; set; }
    public string SplitCompliance { get; set; } = string.Empty;
    public string Assessment { get; set; } = string.Empty;
}

public class PestDiagnosticDto
{
    public string Status { get; set; } = "Safe"; // "Safe", "MonitoringRequired", "HighRisk", "SafetyAlert"
    public int TreatmentsCount { get; set; }
    public List<string> ProductsUsed { get; set; } = new();
    public List<string> TargetPests { get; set; } = new();
    public int DaysSinceLastTreatment { get; set; }
    public bool BannedChemicalDetected { get; set; }
    public string Assessment { get; set; } = string.Empty;
}

public class OtherDiagnosticDto
{
    public int ActivitiesCount { get; set; }
    public List<string> ActivityTypes { get; set; } = new();
    public string WeedingStatus { get; set; } = "Not Recorded";
    public string Assessment { get; set; } = string.Empty;
}

public class ActivityRecommendationDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public int? DbId { get; set; }
    public string Category { get; set; } = "General"; // "Irrigation", "Fertilizer", "Pest", "General"
    public string Priority { get; set; } = "MEDIUM"; // "HIGH", "MEDIUM", "LOW"
    public string Action { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Evidence { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; } = 0.85;
    public List<CitationDto> Citations { get; set; } = new();
    public bool RequiresOfficerReview { get; set; } = true;

    // Status: "PENDING_OFFICER_REVIEW", "APPROVED", "REJECTED", "REVISION_REQUESTED", "EXECUTED"
    public string Status { get; set; } = "PENDING_OFFICER_REVIEW";
    public string? ExecutionPayloadJson { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
    public int? ExecutedActivityId { get; set; }
    public DateTime? ExecutedAt { get; set; }
}

public class ReviewRecommendationRequestDto
{
    public string RecommendationId { get; set; } = string.Empty;
    public string Decision { get; set; } = "Approve"; // "Approve", "Reject", "Revise", "Execute"
    public string? Notes { get; set; }
    public string? CustomDate { get; set; }
    public string? RecommendationJson { get; set; }
}

public class ReviewRecommendationResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ActivityRecommendationDto? Recommendation { get; set; }
    public int? CreatedActivityId { get; set; }
}

public class OfficerRecommendationReviewDto
{
    public string Decision { get; set; } = "Approve"; // "Approve", "Reject"
    public string? Comment { get; set; }
}

public class CropActivityRecommendationDto
{
    public int Id { get; set; }
    public string RecommendationUid { get; set; } = string.Empty;
    public int CultivationCycleId { get; set; }
    public string CycleName { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string FarmerName { get; set; } = string.Empty;
    public int FarmerId { get; set; }
    public string DivisionName { get; set; } = string.Empty;
    public string VarietyName { get; set; } = string.Empty;
    public int DaysAfterSowing { get; set; }
    public string Stage { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Evidence { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public string CitationsJson { get; set; } = "[]";
    public bool RequiresOfficerReview { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ExecutionPayloadJson { get; set; }

    public int? OfficerId { get; set; }
    public string? OfficerName { get; set; }
    public string? OfficerComment { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public int? ExecutedActivityId { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CitationDto
{
    public string Document { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
}

public class AiChatRequestDto
{
    public int CultivationCycleId { get; set; }
    public string Question { get; set; } = string.Empty;
    public List<ChatMessageDto>? History { get; set; }
}

public class ChatMessageDto
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
}

public class AiChatResponseDto
{
    public string Answer { get; set; } = string.Empty;
    public List<string>? SuggestedFollowUps { get; set; }
    public bool RequiresOfficerReview { get; set; }
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
}

public class AgentAuditLogEntryDto
{
    public int Id { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string InputJson { get; set; } = string.Empty;
    public string ToolCallsJson { get; set; } = string.Empty;
    public string RawOutput { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int DurationMs { get; set; }
    public DateTime CreatedAt { get; set; }
}
