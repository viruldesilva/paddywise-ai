using System.ComponentModel.DataAnnotations;

namespace PaddyWise.Api.DTOs.FieldCultivation;

public class CreateFieldRequestDto
{
    [Required(ErrorMessage = "Field name is required.")]
    [MaxLength(100, ErrorMessage = "Field name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 1000, ErrorMessage = "Area must be between 0.01 and 1000 acres.")]
    public decimal Area { get; set; }

    [MaxLength(50, ErrorMessage = "Soil type cannot exceed 50 characters.")]
    public string SoilType { get; set; } = string.Empty;

    [MaxLength(50, ErrorMessage = "Irrigation type cannot exceed 50 characters.")]
    public string IrrigationType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Division is required.")]
    public int DivisionId { get; set; }

    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public double? Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public double? Longitude { get; set; }
}
