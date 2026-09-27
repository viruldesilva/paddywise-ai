namespace PaddyWise.Api.Services.ReportingApproval.Agents;

public class AgentValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Violations { get; set; } = new();
    public bool RequiresOfficerReview { get; set; }
}

public interface IValidationAgentService
{
    Task<AgentValidationResult> ValidatePestDiseaseReportAsync(int reportId, CancellationToken ct = default);
    Task<AgentValidationResult> ValidateCultivationPlanAsync(int planId, CancellationToken ct = default);
}
