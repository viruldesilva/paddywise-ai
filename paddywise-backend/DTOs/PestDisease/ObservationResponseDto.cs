namespace PaddyWise.Api.DTOs.PestDisease;

public class ObservationResponseDto
{
    public int Id { get; set; }

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

    /// <summary>Null when never analyzed. When set with an empty Reports list, the agent ran
    /// and found no likely match — distinct from "not yet analyzed."</summary>
    public DateTime? LastAnalyzedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<PestDiseaseReportSummaryDto> Reports { get; set; } = new();
}
