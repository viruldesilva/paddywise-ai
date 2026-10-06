using PaddyWise.Api.Agents.CropResource;
using PaddyWise.Api.Entities.CropResource;
using PaddyWise.Backend.Tests.CropResource.Helpers;
using PaddyWise.Backend.Tests.FieldCultivation.Helpers;
using Xunit;

namespace PaddyWise.Backend.Tests.CropResource.Agent;

/// <summary>
/// CropActivityTools — the read-only data access the Resource Analysis agent's
/// recommendations are built from: stage arithmetic and the activity bundle.
/// </summary>
[Trait("Component", "CropResource")]
public class CropActivityToolsTests
{
    [Theory]
    [InlineData(0, "Nursery / Establishment")]
    [InlineData(14, "Nursery / Establishment")]
    [InlineData(15, "Tillering")]
    [InlineData(54, "Tillering")]                  // 45 % of 120
    [InlineData(55, "Panicle Initiation")]
    [InlineData(78, "Panicle Initiation")]         // 65 %
    [InlineData(79, "Flowering")]
    [InlineData(96, "Flowering")]                  // 80 %
    [InlineData(97, "Grain Filling / Ripening")]
    [InlineData(109, "Grain Filling / Ripening")]  // duration − 11
    [InlineData(110, "Harvest Ready")]             // duration − 10
    [InlineData(120, "Harvest Ready")]
    [InlineData(121, "Harvested")]
    public void AG07_CalculateCurrentStage_Boundaries(int das, string expected)
    {
        Assert.Equal(expected, CropActivityTools.CalculateCurrentStage(das, 120));
    }

    [Fact]
    public async Task AG08a_TheBundleParsesEachActivityTypeAndSkipsMalformedDetails()
    {
        using var db = FcTestDb.CreateContext();
        var cycle = await FcTestDb.SeedFarmerCycleAsync(db, 1, "QA Farmer");
        var today = CrSeed.Today;
        await CrSeed.AddActivityAsync(db, cycle, 1, CropActivityType.Irrigation, today.AddDays(-3), CrSeed.Irrigation(4.5, 3, "Tank"));
        await CrSeed.AddActivityAsync(db, cycle, 1, CropActivityType.Fertilizer, today.AddDays(-2), CrSeed.Fertilizer("Urea", "45.5"));
        await CrSeed.AddActivityAsync(db, cycle, 1, CropActivityType.Pesticide, today.AddDays(-1), CrSeed.Pesticide("Fipronil 50 SC"));
        await CrSeed.AddActivityAsync(db, cycle, 1, CropActivityType.Other, today, CrSeed.Other("Weeding"));
        await CrSeed.AddActivityAsync(db, cycle, 1, CropActivityType.Fertilizer, today, "{ not json");

        var bundle = await new CropActivityTools(db).GetCycleActivityBundleAsync(cycle.Id);

        Assert.NotNull(bundle);
        Assert.Equal(4.5, Assert.Single(bundle!.Irrigations).WaterLevelCm);
        Assert.Equal(45.5, Assert.Single(bundle.Fertilizers).QuantityKgPerHa); // numeric string accepted
        Assert.Equal("Fipronil 50 SC", Assert.Single(bundle.Pesticides).Product);
        Assert.Equal("Weeding", Assert.Single(bundle.Others).SpecificActivity);
        Assert.Equal(20, bundle.DaysAfterSowing);
        Assert.Equal("Tillering", bundle.EstimatedStage);
    }

    [Fact]
    public async Task AG08b_AnUnknownCycle_ReturnsNull()
    {
        using var db = FcTestDb.CreateContext();

        Assert.Null(await new CropActivityTools(db).GetCycleActivityBundleAsync(404));
    }

    [Fact]
    public async Task AG08c_DaysAfterSowing_IsNeverNegativeForAFutureSowing()
    {
        using var db = FcTestDb.CreateContext();
        var cycle = await FcTestDb.SeedFarmerCycleAsync(db, 1, "QA Farmer", sownDaysAgo: -10);

        var bundle = await new CropActivityTools(db).GetCycleActivityBundleAsync(cycle.Id);

        Assert.Equal(0, bundle!.DaysAfterSowing);
        Assert.Equal("Nursery / Establishment", bundle.EstimatedStage);
    }

    [Fact]
    public async Task AG08d_TheBundleReadsOnlyTheRequestedCycle_AndWritesNothing()
    {
        using var db = FcTestDb.CreateContext();
        var mine = await FcTestDb.SeedFarmerCycleAsync(db, 1, "QA Farmer");
        var theirs = await FcTestDb.SeedFarmerCycleAsync(db, 2, "Other Farmer");
        await CrSeed.AddActivityAsync(db, theirs, 2, CropActivityType.Pesticide, CrSeed.Today, CrSeed.Pesticide("Carbofuran 3G"));
        db.ChangeTracker.Clear();

        var bundle = await new CropActivityTools(db).GetCycleActivityBundleAsync(mine.Id);

        Assert.Empty(bundle!.Pesticides);
        Assert.Empty(db.ChangeTracker.Entries().Where(e => e.State != Microsoft.EntityFrameworkCore.EntityState.Unchanged));
    }
}
