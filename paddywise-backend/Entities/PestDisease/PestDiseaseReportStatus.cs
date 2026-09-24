namespace PaddyWise.Api.Entities.PestDisease;

/// <summary>Where an agent-generated diagnosis sits between generation and officer sign-off.
/// Mirrors FieldCultivation's PlanStatus shape for consistency across components.</summary>
public enum PestDiseaseReportStatus
{
    PendingOfficerReview = 0,
    Approved = 1,
    Rejected = 2,
    RevisionRequested = 3
}
