namespace PaddyWise.Api.DTOs.FieldCultivation;

/// <summary>One row of the officer's approval queue — enough to triage without opening the plan.</summary>
public class PendingPlanSummaryDto
{
    public int PlanId { get; set; }
    public int CycleId { get; set; }
    public string FarmerName { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string DivisionName { get; set; } = string.Empty;
    public string Objective { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
