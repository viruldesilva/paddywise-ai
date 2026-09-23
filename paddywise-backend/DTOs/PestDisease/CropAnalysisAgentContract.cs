namespace PaddyWise.Api.DTOs.PestDisease;

// The Crop Analysis (Pest & Disease Diagnosis) agent's locked input/output shape, carried
// opaquely inside DelegatedTask.PayloadJson / DelegatedTaskResult.ResultJson so it can be
// serialized for the agent call and the AgentRunLog audit row alike.

public class CropAnalysisAgentInput
{
    public int ObservationId { get; set; }
    public int CultivationId { get; set; }
    public string CropStage { get; set; } = string.Empty;
    public string Symptoms { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

public class CropAnalysisAgentOutput
{
    public List<PossibleIssueCandidate> PossibleIssues { get; set; } = new();
    public string RecommendedNextStep { get; set; } = string.Empty;
}

public class PossibleIssueCandidate
{
    public string Name { get; set; } = string.Empty;

    /// <summary>0.00-1.00. Never presented as certainty.</summary>
    public decimal Confidence { get; set; }

    public string Source { get; set; } = string.Empty;
}
