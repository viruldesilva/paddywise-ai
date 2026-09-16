namespace PaddyWise.Api.Agents.Shared;

/// <summary>
/// Work the Cultivation Planning Agent hands to another component's agent.
/// PayloadJson is deliberately opaque — each component owns the shape it expects.
/// </summary>
public class DelegatedTask
{
    public string TaskType { get; set; } = string.Empty;
    public int CultivationCycleId { get; set; }
    public int? CultivationPlanId { get; set; }
    public string PayloadJson { get; set; } = "{}";
}

/// <summary>What a delegated agent hands back.</summary>
public class DelegatedTaskResult
{
    public string Note { get; set; } = string.Empty;
    public string? ResultJson { get; set; }
}

/// <summary>
/// DI keys for the delegated agents. Real implementations replace the stubs by
/// registering against the same key.
/// </summary>
public static class AgentNames
{
    public const string CultivationPlanning = "CultivationPlanningAgent";
    public const string ResourceAnalysis = "ResourceAnalysisAgent";
    public const string PestDiseaseDiagnosis = "PestDiseaseDiagnosisAgent";
    public const string SchedulingValidation = "SchedulingValidationAgent";
}
