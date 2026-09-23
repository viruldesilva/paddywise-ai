using System;
using System.Collections.Generic;

namespace PaddyWise.Api.Agents.CropResource;

public class ActivityAnalysisInput
{
    public int CultivationCycleId { get; set; }
    public string? FarmerQuestion { get; set; }
    public string? FocusArea { get; set; } // "All", "Irrigation", "Fertilizer", "Pesticide"
}

public class CropActivityAnalysisOutput
{
    public FieldOverviewDto FieldOverview { get; set; } = new();
    public DiagnosticsSummaryDto Diagnostics { get; set; } = new();
    public List<ActivityRecommendationDto> Recommendations { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public bool RequiresOfficerReview { get; set; }
    public string ExecutiveSummary { get; set; } = string.Empty;
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
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
    public string Category { get; set; } = "General"; // "Irrigation", "Fertilizer", "Pest", "General"
    public string Priority { get; set; } = "MEDIUM"; // "HIGH", "MEDIUM", "LOW"
    public string Action { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Evidence { get; set; } = string.Empty; // e.g. "Triggered by Urea applied on Sept 18 (20 DAS)"
    public double ConfidenceScore { get; set; } = 0.85; // 0.0 - 1.0
    public List<CitationDto> Citations { get; set; } = new();
    public bool RequiresOfficerReview { get; set; }
}

public class CitationDto
{
    public string Document { get; set; } = string.Empty; // e.g. "Department of Agriculture Sri Lanka - Paddy Cultivation Guide"
    public string Section { get; set; } = string.Empty; // e.g. "Split Application of Urea for 3.5 Month Varieties"
}

public class AiChatRequestDto
{
    public int CultivationCycleId { get; set; }
    public string Question { get; set; } = string.Empty;
    public List<ChatMessageDto>? History { get; set; }
}

public class ChatMessageDto
{
    public string Role { get; set; } = "user"; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
}

public class AiChatResponseDto
{
    public string Answer { get; set; } = string.Empty;
    public List<string>? SuggestedFollowUps { get; set; }
    public bool RequiresOfficerReview { get; set; }
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
}
