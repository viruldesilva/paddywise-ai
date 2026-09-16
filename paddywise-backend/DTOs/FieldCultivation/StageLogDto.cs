namespace PaddyWise.Api.DTOs.FieldCultivation;

public class StageLogDto
{
    public int Id { get; set; }
    public string Stage { get; set; } = string.Empty;
    public DateOnly ObservedOn { get; set; }
    public string? Notes { get; set; }
    public int LoggedByUserId { get; set; }
    public string LoggedByUserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
