namespace PaddyWise.Api.DTOs.FieldCultivation;

public class StageWindowDto
{
    public string Stage { get; set; } = string.Empty;
    public DateOnly Start { get; set; }
    public DateOnly End { get; set; }
}
