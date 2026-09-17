using System.ComponentModel.DataAnnotations;

namespace PaddyWise.Api.DTOs.FieldCultivation;

public class LogStageRequestDto
{
    [Required(ErrorMessage = "Stage is required.")]
    public string Stage { get; set; } = string.Empty; // "Nursery" ... "Harvest"

    [Required(ErrorMessage = "Observation date is required.")]
    public DateOnly ObservedOn { get; set; }

    [MaxLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
    public string? Notes { get; set; }
}
