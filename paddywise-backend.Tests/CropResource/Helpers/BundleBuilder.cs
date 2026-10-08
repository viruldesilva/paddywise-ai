using PaddyWise.Api.Agents.CropResource;
using PaddyWise.Api.Entities.FieldCultivation;

namespace PaddyWise.Backend.Tests.CropResource.Helpers;

/// <summary>
/// Builds a CycleActivityBundle directly for ActivitySafetyValidator tests: a fixed "today"
/// so every boundary is an exact date, and the expected harvest far away unless a test is
/// about the pre-harvest interval.
/// </summary>
public sealed class BundleBuilder
{
    public static readonly DateOnly Today = new(2026, 6, 15);

    private int _daysToHarvest = 60;
    private string _stage = "Tillering";
    private readonly List<ParsedFertilizer> _fertilizers = new();
    private readonly List<ParsedPesticide> _pesticides = new();
    private readonly List<ParsedIrrigation> _irrigations = new();

    public BundleBuilder HarvestIn(int days) { _daysToHarvest = days; return this; }
    public BundleBuilder AtStage(string stage) { _stage = stage; return this; }

    public BundleBuilder Fertilizer(string type, double kgPerHa, int daysAgo)
    {
        _fertilizers.Add(new ParsedFertilizer { Type = type, QuantityKgPerHa = kgPerHa, Date = Today.AddDays(-daysAgo) });
        return this;
    }

    public BundleBuilder Pesticide(string product, int daysAgo)
    {
        _pesticides.Add(new ParsedPesticide { Product = product, TargetPest = "Stem borer", Date = Today.AddDays(-daysAgo) });
        return this;
    }

    /// <summary>Irrigations must be added oldest first, as CropActivityTools orders them.</summary>
    public BundleBuilder Irrigation(double? waterLevelCm, int daysAgo)
    {
        _irrigations.Add(new ParsedIrrigation { WaterLevelCm = waterLevelCm, DurationHours = 2, Date = Today.AddDays(-daysAgo) });
        return this;
    }

    public CycleActivityBundle Build() => new()
    {
        Cycle = new CultivationCycle
        {
            Id = 1,
            SowingDate = Today.AddDays(-60),
            ExpectedHarvestDate = Today.AddDays(_daysToHarvest)
        },
        Today = Today,
        DaysAfterSowing = 60,
        EstimatedStage = _stage,
        VarietyDurationDays = 120,
        Fertilizers = _fertilizers,
        Pesticides = _pesticides,
        Irrigations = _irrigations
    };
}
