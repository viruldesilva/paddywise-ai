using System.ComponentModel.DataAnnotations;

namespace PaddyWise.Api.DTOs.FieldCultivation;

public class CreateCycleRequestDto
{
    // Ignored by POST /api/fields/{fieldId}/start-cultivation, which takes the
    // field from the route; the service is given the resolved id either way.
    public int FieldId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Variety is required.")]
    public int VarietyId { get; set; }

    [Required(ErrorMessage = "Season is required.")]
    public string Season { get; set; } = string.Empty; // "Yala" or "Maha"

    [Range(2000, 2100, ErrorMessage = "Year must be between 2000 and 2100.")]
    public int Year { get; set; }

    [Required(ErrorMessage = "Cultivation method is required.")]
    public string Method { get; set; } = string.Empty; // "Broadcasting", "Transplanting", "DirectSeeding"

    [Required(ErrorMessage = "Sowing date is required.")]
    public DateOnly SowingDate { get; set; }

    [MaxLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
    public string? Notes { get; set; }
}
