namespace PaddyWise.Api.DTOs.FieldCultivation;

public class FieldResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Area { get; set; }
    public string SoilType { get; set; } = string.Empty;
    public string IrrigationType { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int DivisionId { get; set; }
    public string DivisionName { get; set; } = string.Empty;
    public int FarmerId { get; set; }
    public string FarmerName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
