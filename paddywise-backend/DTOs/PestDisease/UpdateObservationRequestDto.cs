using System.ComponentModel.DataAnnotations;

namespace PaddyWise.Api.DTOs.PestDisease;

/// <summary>The farmer correcting their own report — the cycle it belongs to cannot change.</summary>
public class UpdateObservationRequestDto
{
    [Required(ErrorMessage = "Observation type is required.")]
    public string ObservationType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Symptoms are required.")]
    [MaxLength(2000, ErrorMessage = "Symptoms cannot exceed 2000 characters.")]
    public string Symptoms { get; set; } = string.Empty;

    [Required(ErrorMessage = "Severity is required.")]
    public string Severity { get; set; } = string.Empty;

    [Url(ErrorMessage = "Image URL must be a valid URL.")]
    public string? ImageUrl { get; set; }
}
