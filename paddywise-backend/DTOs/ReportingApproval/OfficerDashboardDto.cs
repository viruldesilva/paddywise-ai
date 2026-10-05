namespace PaddyWise.Api.DTOs.ReportingApproval;

public class OfficerDashboardDto
{
    public AssignedDivisionDto AssignedDivision { get; set; } = new();
    public int PendingApprovalCount { get; set; }
    public int ApprovedTreatmentsThisSeason { get; set; }
    public string CultivationSeason { get; set; } = string.Empty;
    public int RegisteredFarmerCount { get; set; }
    public int? GnDivisionCount { get; set; }
    public List<PendingTreatmentPlanDto> PendingQueue { get; set; } = new();
}

public class AssignedDivisionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Centre { get; set; } = "Agrarian Services Centre";
    public string District { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
}

public class PendingTreatmentPlanDto
{
    public int Id { get; set; }
    public string FarmerName { get; set; } = string.Empty;
    public string GnDivision { get; set; } = string.Empty;
    public string Symptom { get; set; } = string.Empty;
    public string AiDiagnosis { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public string ProposedPlan { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
