namespace PaddyWise.Api.DTOs.FieldCultivation;

public class CycleResponseDto
{
    public int Id { get; set; }

    public int FieldId { get; set; }
    public string FieldName { get; set; } = string.Empty;

    public int VarietyId { get; set; }
    public string VarietyName { get; set; } = string.Empty;
    public int DurationDays { get; set; }

    public string Season { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Method { get; set; } = string.Empty;

    public DateOnly SowingDate { get; set; }
    public DateOnly ExpectedHarvestDate { get; set; }
    public DateOnly? ActualHarvestDate { get; set; }

    // The stage the farmer has actually reported.
    public string CurrentStage { get; set; } = string.Empty;

    // The stage the plan says the cycle should be in today — the two differing is
    // the signal the planning agent acts on.
    public string ExpectedStageToday { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<StageWindowDto> Timeline { get; set; } = new();
    public List<StageLogDto> StageLogs { get; set; } = new();
}
