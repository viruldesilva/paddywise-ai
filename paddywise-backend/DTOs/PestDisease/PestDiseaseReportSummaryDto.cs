namespace PaddyWise.Api.DTOs.PestDisease;

/// <summary>One candidate diagnosis, as shown on an observation's detail view.</summary>
public class PestDiseaseReportSummaryDto
{
    public int Id { get; set; }
    public string PossibleIssue { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? OfficerComment { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
