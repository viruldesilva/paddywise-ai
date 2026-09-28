namespace PaddyWise.Api.Services.ReportingApproval;

/// <summary>
/// Service for generating AI-assisted draft revision comments for agricultural officers
/// to review and edit before sending feedback to farmers.
/// </summary>
public interface IRevisionDraftService
{
    /// <summary>
    /// Generates a draft revision comment for a CultivationPlan based on its validation errors.
    /// If there are no validation errors or the LLM call fails, returns a safe fallback message.
    /// </summary>
    Task<string> DraftPlanRevisionCommentAsync(int planId, CancellationToken ct = default);

    /// <summary>
    /// Generates a draft revision comment for a PestDiseaseReport based on the crop observation.
    /// If the LLM call fails or times out, returns a safe fallback message.
    /// </summary>
    Task<string> DraftReportRevisionCommentAsync(int reportId, CancellationToken ct = default);
}
