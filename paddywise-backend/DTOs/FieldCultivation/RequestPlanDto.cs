using System.ComponentModel.DataAnnotations;

namespace PaddyWise.Api.DTOs.FieldCultivation;

/// <summary>What the farmer wants out of this cultivation cycle, in their own words.</summary>
public class RequestPlanDto
{
    [Required(ErrorMessage = "Objective is required.")]
    [MaxLength(1000, ErrorMessage = "Objective cannot exceed 1000 characters.")]
    public string Objective { get; set; } = string.Empty;
}
