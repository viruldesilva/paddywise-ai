namespace PaddyWise.Api.DTOs.FieldCultivation;

public class VarietyResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public string AgeGroup { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
