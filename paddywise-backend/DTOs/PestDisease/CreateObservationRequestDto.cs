using System.ComponentModel.DataAnnotations;

namespace PaddyWise.Api.DTOs.PestDisease;

public class CreateObservationRequestDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Cultivation cycle is required.")]
    public int CultivationCycleId { get; set; }

    [Required(ErrorMessage = "Observation type is required.")]
    public string ObservationType { get; set; } = string.Empty; // "Pest", "Disease" or "Unknown"

    [Required(ErrorMessage = "Symptoms are required.")]
    [MaxLength(2000, ErrorMessage = "Symptoms cannot exceed 2000 characters.")]
    public string Symptoms { get; set; } = string.Empty;

    [Required(ErrorMessage = "Severity is required.")]
    public string Severity { get; set; } = string.Empty; // "Low", "Moderate" or "Severe"

    [Url(ErrorMessage = "Image URL must be a valid URL.")]
    public string? ImageUrl { get; set; }
}
