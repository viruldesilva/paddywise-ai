using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Api.Entities.PestDisease;

/// <summary>One candidate match the diagnosis agent found for an observation. A single agent
/// run against one CropObservation can produce several of these (one per possible issue), each
/// checked against the PestDiseaseKnowledge table rather than invented.</summary>
public class PestDiseaseReport
{
    public int Id { get; set; }

    public int CropObservationId { get; set; }

    /// <summary>Name of the matched entry in PestDiseaseKnowledge, or the agent's best-guess
    /// label when nothing in the knowledge base matches closely.</summary>
    public string PossibleIssue { get; set; } = string.Empty;

    /// <summary>0.00–1.00. Never presented as certainty — "possible match with N% confidence."</summary>
    public decimal Confidence { get; set; }

    public PestDiseaseReportStatus Status { get; set; }

    // The officer who reviewed it — null until someone picks it up.
    public int? OfficerId { get; set; }
    public string? OfficerComment { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public CropObservation CropObservation { get; set; } = null!;
    public User? Officer { get; set; }
}
