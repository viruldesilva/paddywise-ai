namespace PaddyWise.Api.Entities.PestDisease;

/// <summary>One Crop Analysis (Pest &amp; Disease Diagnosis) agent run, kept for audit and
/// debugging. Written whether the run succeeded or not. Mirrors FieldCultivation's
/// AgentRunLog shape, but this component keeps its own row rather than reusing that entity
/// (it FKs to CultivationPlan, which is Component 1's).</summary>
public class DiagnosisRunLog
{
    public int Id { get; set; }

    public int? CropObservationId { get; set; }

    public string AgentName { get; set; } = string.Empty;

    /// <summary>Ties every log line of a single request together.</summary>
    public string CorrelationId { get; set; } = string.Empty;

    public string InputJson { get; set; } = "{}";
    public string ToolCallsJson { get; set; } = "[]";

    /// <summary>The agent's unparsed reply, kept verbatim so a bad parse can be diagnosed.</summary>
    public string RawOutput { get; set; } = string.Empty;

    public bool Success { get; set; }
    public string? Error { get; set; }

    public int DurationMs { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public CropObservation? CropObservation { get; set; }
}
