using System.ComponentModel.DataAnnotations;

namespace PaddyWise.Api.DTOs.PestDisease;

/// <summary>An officer's verdict on a diagnosis waiting for review.</summary>
public class ReviewPestDiseaseReportDto
{
    /// <summary>"Approve", "Reject" or "RequestRevision".</summary>
    [Required(ErrorMessage = "Decision is required.")]
    public string Decision { get; set; } = string.Empty;

    /// <summary>Required when rejecting or asking for a revision — the farmer needs the reason.</summary>
    [MaxLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
    public string? Comment { get; set; }
}

/// <summary>What an officer can decide about a diagnosis in front of them.</summary>
public enum PestDiseaseReportReviewDecision
{
    Approve = 0,
    Reject = 1,
    RequestRevision = 2
}
