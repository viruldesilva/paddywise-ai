using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Api.Entities.FieldCultivation;

public class Field
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    // Acres — the unit Sri Lankan paddy farmers state field size in.
    public decimal Area { get; set; }
    public string SoilType { get; set; } = string.Empty;
    public string IrrigationType { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Ownership: the Farmer this field belongs to, and the agrarian division it sits in.
    public int FarmerId { get; set; }
    public int DivisionId { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User Farmer { get; set; } = null!;
    public Division Division { get; set; } = null!;
}
