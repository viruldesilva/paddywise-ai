namespace PaddyWise.Api.Entities.FieldCultivation; 
public class Field { 
    public int Id { get; set; } 
    public string Name { get; set; } = string.Empty; 
    public decimal Area { get; set; } 
    public string SoilType { get; set; } = string.Empty; 
    public string IrrigationType { get; set; } = string.Empty; 
    public double? Latitude { get; set; } public double? Longitude { get; set; } 
}