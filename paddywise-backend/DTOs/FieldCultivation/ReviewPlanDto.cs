using System.ComponentModel.DataAnnotations;

namespace PaddyWise.Api.DTOs.FieldCultivation;

/// <summary>An officer's verdict on a plan waiting for approval.</summary>
public class ReviewPlanDto
{
    /// <summary>"Approve", "Reject" or "RequestRevision".</summary>
    [Required(ErrorMessage = "Decision is required.")]
    public string Decision { get; set; } = string.Empty;

    /// <summary>Required when rejecting or asking for a revision — the farmer needs the reason.</summary>
    [MaxLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
    public string? Comment { get; set; }
}

/// <summary>What an officer can decide about a plan in front of them.</summary>
public enum PlanReviewDecision
{
    Approve = 0,
    Reject = 1,
    RequestRevision = 2
}
