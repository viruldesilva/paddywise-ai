namespace PaddyWise.Api.Entities.FieldCultivation;

/// <summary>
/// One agent run, kept for audit and debugging. Written whether the run succeeded or not,
/// and not tied to a plan when the run never produced one.
/// </summary>
public class AgentRunLog
{
    public int Id { get; set; }

    public int? CultivationPlanId { get; set; }

    public string AgentName { get; set; } = string.Empty;

    /// <summary>Ties every log line of a single request together.</summary>
    public string CorrelationId { get; set; } = string.Empty;

    public string InputJson { get; set; } = "{}";
    public string ToolCallsJson { get; set; } = "[]";

    /// <summary>The model's unparsed reply, kept verbatim so a bad parse can be diagnosed.</summary>
    public string RawOutput { get; set; } = string.Empty;

    public bool Success { get; set; }
    public string? Error { get; set; }

    public int DurationMs { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public CultivationPlan? CultivationPlan { get; set; }
}
