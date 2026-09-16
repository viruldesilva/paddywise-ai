using System.ComponentModel.DataAnnotations;

namespace PaddyWise.Api.DTOs.FieldCultivation;

public class UpdateCycleStatusRequestDto
{
    [Required(ErrorMessage = "Status is required.")]
    public string Status { get; set; } = string.Empty; // "Planned", "Active", "Harvested", "Abandoned"
}
