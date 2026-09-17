namespace PaddyWise.Api.Entities.FieldCultivation;

/// <summary>One season's cultivation on a single field.</summary>
public class CultivationCycle
{
    public int Id { get; set; }

    public int FieldId { get; set; }
    public int VarietyId { get; set; }

    public Season Season { get; set; }
    public int Year { get; set; }
    public CultivationMethod Method { get; set; }

    public DateOnly SowingDate { get; set; }

    // Stored, not computed by the database: the service sets this at creation time
    // to SowingDate + Variety.DurationDays, so a later change to the variety's
    // duration does not silently move an existing cycle's plan.
    public DateOnly ExpectedHarvestDate { get; set; }

    public DateOnly? ActualHarvestDate { get; set; }

    public GrowthStage CurrentStage { get; set; }
    public CycleStatus Status { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Field Field { get; set; } = null!;
    public Variety Variety { get; set; } = null!;
}
