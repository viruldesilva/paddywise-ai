namespace PaddyWise.Api.DTOs.PestDisease;

/// <summary>Full detail of one agent-generated diagnosis, for the officer review queue.</summary>
public class PestDiseaseReportResponseDto
{
    public int Id { get; set; }
    public int CropObservationId { get; set; }
    public int CultivationCycleId { get; set; }
    public int FieldId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public int ReportedByUserId { get; set; }
    public string ReportedByUserName { get; set; } = string.Empty;
    public string ObservationType { get; set; } = string.Empty;
    public string CropStage { get; set; } = string.Empty;
    public string Symptoms { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string PossibleIssue { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? OfficerId { get; set; }
    public string? OfficerName { get; set; }
    public string? OfficerComment { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
