using PaddyWise.Api.Entities.ReportingApproval;

namespace PaddyWise.Api.Services.ReportingApproval;

/// <summary>
/// Service for generating plain-language, farmer-friendly notification messages using LLM
/// when an Agricultural Officer reviews a CultivationPlan or PestDiseaseReport.
/// </summary>
public interface INotificationMessageService
{
    /// <summary>
    /// Generates a plain-language explanation of an officer's decision on a CultivationPlan,
    /// creates a Notification for the requesting farmer, and records an AgentRunLog.
    /// Never throws; falls back to a deterministic template message if LLM fails.
    /// </summary>
    Task<Notification?> GenerateAndCreateCultivationPlanNotificationAsync(int planId, CancellationToken ct = default);

    /// <summary>
    /// Generates a plain-language explanation of an officer's decision on a PestDiseaseReport,
    /// creates a Notification for the reporting farmer, and records a DiagnosisRunLog.
    /// Never throws; falls back to a deterministic template message if LLM fails.
    /// </summary>
    Task<Notification?> GenerateAndCreatePestDiseaseReportNotificationAsync(int reportId, CancellationToken ct = default);
}
