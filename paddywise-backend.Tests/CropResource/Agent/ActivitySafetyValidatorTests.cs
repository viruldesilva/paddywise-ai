using PaddyWise.Api.Agents.CropResource;
using PaddyWise.Backend.Tests.CropResource.Helpers;
using Xunit;

namespace PaddyWise.Backend.Tests.CropResource.Agent;

/// <summary>
/// ActivitySafetyValidator called directly — the deterministic gate between the agent's
/// recommendations and the farmer. Every rule is tested on both sides of its boundary, with
/// a fixed "today" (BundleBuilder) so each boundary is an exact date.
/// </summary>
[Trait("Component", "CropResource")]
public class ActivitySafetyValidatorTests
{
    private static List<ActivityRecommendationDto> Recs(params (string Category, string Action)[] items) =>
        items.Select(i => new ActivityRecommendationDto { Category = i.Category, Action = i.Action }).ToList();

    // ---------------------------------------------------------------- banned substances

    [Theory]
    [InlineData("Carbofuran")]
    [InlineData("Chlorpyrifos")]
    [InlineData("Paraquat")]
    [InlineData("Monocrotophos")]
    [InlineData("Endosulfan")]
    [InlineData("Dimethoate")]
    [InlineData("Methamidophos")]
    [InlineData("Propanil")]
    [InlineData("Glyphosate")]
    [InlineData("DDT")]
    [InlineData("Aldrin")]
    public void AG01a_EachBannedSubstance_IsACriticalHazardNeedingOfficerReview(string substance)
    {
        var bundle = new BundleBuilder().Pesticide(substance, daysAgo: 30).Build();

        var result = ActivitySafetyValidator.AuditActivitiesAndRecommendations(bundle, Recs());

        Assert.True(result.HasCriticalHazard);
        Assert.True(result.RequiresOfficerReview);
        var alert = Assert.Single(result.SafetyAlerts);
        Assert.StartsWith("CRITICAL REGULATORY ALERT", alert);
        Assert.Contains($"contains {substance}", alert);
    }

    [Theory]
    [InlineData("carbofuran")]       // case-insensitive
    [InlineData("Carbofuran 3G")]    // brand/formulation around the active ingredient
    [InlineData("Super PARAQUAT 20 SL")]
    public void AG01b_BannedSubstances_MatchCaseInsensitivelyInsideAProductName(string product)
    {
        var bundle = new BundleBuilder().Pesticide(product, daysAgo: 30).Build();

        var result = ActivitySafetyValidator.AuditActivitiesAndRecommendations(bundle, Recs());

        Assert.True(result.HasCriticalHazard);
    }

    [Fact]
    public void AG01c_AProductWithNoBannedSubstance_IsNotFlagged()
    {
        var bundle = new BundleBuilder().Pesticide("Fipronil 50 SC", daysAgo: 30).Build();

        var result = ActivitySafetyValidator.AuditActivitiesAndRecommendations(bundle, Recs());

        Assert.False(result.HasCriticalHazard);
        Assert.False(result.RequiresOfficerReview);
        Assert.Empty(result.SafetyAlerts);
    }

    // ---------------------------------------------------------------- pre-harvest interval

    [Theory]
    [InlineData(14, true)]   // last day inside the 14-day window
    [InlineData(15, false)]  // one day outside
    [InlineData(0, true)]    // harvest day itself
    [InlineData(-1, false)]  // expected harvest already passed
    public void AG02_PreHarvestWindow_Is0To14DaysBeforeExpectedHarvest(int daysToHarvest, bool windowActive)
    {
        var bundle = new BundleBuilder().HarvestIn(daysToHarvest).Build();
        var recs = Recs(("Fertilizer", "Apply Urea."));

        ActivitySafetyValidator.AuditActivitiesAndRecommendations(bundle, recs);

        Assert.Equal(windowActive, recs.Any(r => r.Action.StartsWith("Do NOT apply any chemical")));
        Assert.Equal(!windowActive, recs.Any(r => r.Category == "Fertilizer"));
    }

    [Theory]
    [InlineData(7, true)]   // pesticide exactly 7 days ago counts
    [InlineData(8, false)]  // 8 days ago does not
    public void AG03_InsidePhi_ARecentPesticideRaisesThePhiWarning(int pesticideDaysAgo, bool warned)
    {
        var bundle = new BundleBuilder().HarvestIn(10).Pesticide("Fipronil 50 SC", pesticideDaysAgo).Build();

        var result = ActivitySafetyValidator.AuditActivitiesAndRecommendations(bundle, Recs());

        Assert.Equal(warned, result.SafetyAlerts.Any(a => a.StartsWith("PRE-HARVEST INTERVAL (PHI) WARNING")));
        Assert.Equal(warned, result.RequiresOfficerReview);
    }

    [Fact]
    public void AG04_InsidePhi_FertilizerAndPestRecommendationsAreReplacedByAStopOrder()
    {
        var bundle = new BundleBuilder().HarvestIn(5).Build();
        var recs = Recs(("Fertilizer", "Apply MOP."), ("Pest", "Spray for leaf folder."), ("Irrigation", "Drain the field."));

        ActivitySafetyValidator.AuditActivitiesAndRecommendations(bundle, recs);

        Assert.DoesNotContain(recs, r => r.Category is "Fertilizer" or "Pest");
        Assert.Contains(recs, r => r.Action == "Drain the field.");
        var stop = Assert.Single(recs, r => r.Category == "General");
        Assert.Equal("HIGH", stop.Priority);
        Assert.Contains("(5 days away)", stop.Evidence);
    }

    // ---------------------------------------------------------------- nitrogen limit

    [Theory]
    [InlineData(65.0, false)]  // exactly the single-split maximum
    [InlineData(65.1, true)]   // just over
    public void AG05a_UreaInTheLast10Days_AlertsOnlyAbove65KgPerHa(double kgPerHa, bool alerted)
    {
        var bundle = new BundleBuilder().Fertilizer("Urea", kgPerHa, daysAgo: 2).Build();
        var recs = Recs(("Fertilizer", "Apply 1st Top Dressing of Urea at 50 kg/ha."));

        var result = ActivitySafetyValidator.AuditActivitiesAndRecommendations(bundle, recs);

        Assert.Equal(alerted, result.SafetyAlerts.Any(a => a.StartsWith("NITROGEN EXCESS ALERT")));
        if (alerted)
        {
            Assert.Equal("Suspend all Nitrogen / Urea applications for at least 14 days.", recs[0].Action);
            Assert.DoesNotContain(recs, r => r.Action.StartsWith("Apply"));
        }
        else
        {
            Assert.Contains(recs, r => r.Action.StartsWith("Apply 1st Top Dressing"));
        }
    }

    [Theory]
    [InlineData(10, true)]   // a dose exactly 10 days ago still counts
    [InlineData(11, false)]  // 11 days ago drops out of the window
    public void AG05b_TheUreaWindowIs10DaysInclusive(int secondDoseDaysAgo, bool alerted)
    {
        var bundle = new BundleBuilder()
            .Fertilizer("Urea", 40, daysAgo: secondDoseDaysAgo)
            .Fertilizer("Urea", 40, daysAgo: 1)
            .Build();

        var result = ActivitySafetyValidator.AuditActivitiesAndRecommendations(bundle, Recs());

        Assert.Equal(alerted, result.SafetyAlerts.Any(a => a.Contains("80.0 kg/ha Urea")));
    }

    [Fact]
    public void AG05c_NonUreaFertilizer_DoesNotCountTowardsTheNitrogenLimit()
    {
        var bundle = new BundleBuilder().Fertilizer("MOP", 100, 1).Fertilizer("TSP", 100, 1).Build();

        var result = ActivitySafetyValidator.AuditActivitiesAndRecommendations(bundle, Recs());

        Assert.Empty(result.SafetyAlerts);
    }

    // ---------------------------------------------------------------- moisture at flowering

    [Theory]
    [InlineData(null, 0, true)]   // no irrigation logged at all (level/days unused)
    [InlineData(1.0, 1, true)]    // level at the threshold
    [InlineData(1.01, 1, false)]  // just above it
    [InlineData(3.0, 5, false)]   // last irrigation exactly 5 days ago
    [InlineData(3.0, 6, true)]    // 6 days ago
    public void AG06a_AtFlowering_LowOrStaleWaterRaisesAMoistureAlert(double? level, int daysAgo, bool alerted)
    {
        var builder = new BundleBuilder().AtStage("Flowering");
        if (level != null)
            builder.Irrigation(level, daysAgo);
        var recs = Recs();

        var result = ActivitySafetyValidator.AuditActivitiesAndRecommendations(builder.Build(), recs);

        Assert.Equal(alerted, result.SafetyAlerts.Any(a => a.StartsWith("CRITICAL MOISTURE STRESS ALERT")));
        Assert.Equal(alerted, recs.Any(r => r.Action.StartsWith("Irrigate immediately")));
    }

    [Fact]
    public void AG06b_OutsideFlowering_NoMoistureAlertEvenWithoutIrrigation()
    {
        var bundle = new BundleBuilder().AtStage("Tillering").Build();

        var result = ActivitySafetyValidator.AuditActivitiesAndRecommendations(bundle, Recs());

        Assert.Empty(result.SafetyAlerts);
    }

    [Fact]
    public void AG06c_TheLatestIrrigationIsTheOneJudged()
    {
        // Oldest first, as CropActivityTools orders them: an old dry entry, then a recent wet one.
        var bundle = new BundleBuilder().AtStage("Flowering").Irrigation(0.5, 9).Irrigation(4.0, 1).Build();

        var result = ActivitySafetyValidator.AuditActivitiesAndRecommendations(bundle, Recs());

        Assert.Empty(result.SafetyAlerts);
    }
}
