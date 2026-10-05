using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Services.FieldCultivation;
using Xunit;

namespace PaddyWise.Backend.Tests.FieldCultivation.Agent;

/// <summary>
/// StageTimelineCalculator is what get_stage_timeline returns and what the validator checks
/// against, so its arithmetic is the ground truth every plan is judged by.
/// </summary>
[Trait("Component", "FieldCultivation")]
public class StageTimelineCalculatorTests
{
    private static readonly DateOnly Sowing = new(2026, 1, 1);

    [Theory]
    [InlineData(90)]
    [InlineData(105)]
    [InlineData(120)]
    public void AG21a_SixContiguousWindows_EndingOnHarvestDay(int durationDays)
    {
        var timeline = StageTimelineCalculator.Build(Sowing, durationDays);

        Assert.Equal(6, timeline.Count);
        Assert.Equal(Enum.GetValues<GrowthStage>(), timeline.Select(w => w.Stage));
        Assert.Equal(Sowing, timeline[0].Start);
        Assert.Equal(Sowing.AddDays(durationDays), timeline[^1].End);

        for (var i = 1; i < timeline.Count; i++)
            Assert.Equal(timeline[i - 1].End.AddDays(1), timeline[i].Start);
    }

    [Fact]
    public void AG21b_StageBoundaries_FollowTheSeasonFractions()
    {
        var timeline = StageTimelineCalculator.Build(Sowing, 120);

        // 0.15, 0.40, 0.55, 0.70, 0.95 of 120 days.
        Assert.Equal(Sowing.AddDays(18), timeline[1].Start);
        Assert.Equal(Sowing.AddDays(48), timeline[2].Start);
        Assert.Equal(Sowing.AddDays(66), timeline[3].Start);
        Assert.Equal(Sowing.AddDays(84), timeline[4].Start);
        Assert.Equal(Sowing.AddDays(114), timeline[5].Start);
    }

    [Theory]
    [InlineData(-5, GrowthStage.Nursery)]       // before sowing
    [InlineData(0, GrowthStage.Nursery)]        // sowing day
    [InlineData(18, GrowthStage.Tillering)]     // first Tillering day
    [InlineData(17, GrowthStage.Nursery)]       // last Nursery day
    [InlineData(120, GrowthStage.Harvest)]      // expected harvest day
    [InlineData(200, GrowthStage.Harvest)]      // long after
    public void AG21c_ExpectedStageOn_MatchesTheWindows(int dayOffset, GrowthStage expected)
    {
        var stage = StageTimelineCalculator.ExpectedStageOn(Sowing.AddDays(dayOffset), Sowing, 120);

        Assert.Equal(expected, stage);
    }

    [Fact]
    public void AG21d_ADurationBelowOneDay_IsRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => StageTimelineCalculator.Build(Sowing, 0));
    }
}
