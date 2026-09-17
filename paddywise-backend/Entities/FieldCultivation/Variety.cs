namespace PaddyWise.Api.Entities.FieldCultivation;

/// <summary>
/// A paddy variety released by the Department of Agriculture.
/// NOTE: the seeded DurationDays and AgeGroup values must be verified against
/// official DOA data before this is relied on for harvest-date planning.
/// </summary>
public class Variety
{
    public int Id { get; set; }

    // e.g. "Bg 352"
    public string Name { get; set; } = string.Empty;

    // Days from sowing to harvest; drives a cycle's ExpectedHarvestDate.
    public int DurationDays { get; set; }

    // Farmer-facing grouping: "3 month", "3.5 month", "4 month".
    public string AgeGroup { get; set; } = string.Empty;

    public string? Notes { get; set; }
}
