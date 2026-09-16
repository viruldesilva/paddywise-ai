namespace PaddyWise.Api.Entities.FieldCultivation;

/// <summary>Where a cultivation plan sits between generation and officer sign-off.</summary>
public enum PlanStatus
{
    Draft = 0,
    ValidationFailed = 1,
    PendingOfficerApproval = 2,
    Approved = 3,
    Rejected = 4,
    RevisionRequested = 5
}
