using PaddyWise.Api.Entities.FieldCultivation;

namespace PaddyWise.Api.Services.FieldCultivation;

/// <summary>One stage's planned window, both dates inclusive.</summary>
public readonly record struct StageWindow(GrowthStage Stage, DateOnly Start, DateOnly End);

/// <summary>
/// Turns a sowing date and a variety's duration into the planned growth-stage calendar.
/// Pure arithmetic — no database access — so it can be reused by the planning agent.
/// </summary>
public static class StageTimelineCalculator
{
    // Cumulative fraction of the season at which each stage begins; the trailing
    // 1.0 closes the final stage. Kept in stage order.
    private static readonly (GrowthStage Stage, double StartFraction)[] Stages =
    {
        (GrowthStage.Nursery,           0.00),
        (GrowthStage.Tillering,         0.15),
        (GrowthStage.PanicleInitiation, 0.40),
        (GrowthStage.Flowering,         0.55),
        (GrowthStage.GrainFilling,      0.70),
        (GrowthStage.Harvest,           0.95)
    };

    /// <summary>
    /// Builds the six contiguous stage windows. Window i runs from its own start day
    /// to the day before window i+1 begins; the last ends exactly on
    /// <paramref name="sowingDate"/> + <paramref name="durationDays"/>.
    /// </summary>
    public static IReadOnlyList<StageWindow> Build(DateOnly sowingDate, int durationDays)
    {
        var offsets = BoundaryOffsets(durationDays);
        var windows = new List<StageWindow>(Stages.Length);

        for (var i = 0; i < Stages.Length; i++)
        {
            // The final stage owns the harvest day itself, so it is closed on the
            // boundary rather than the day before the next stage.
            var endOffset = i == Stages.Length - 1
                ? offsets[i + 1]
                : offsets[i + 1] - 1;

            windows.Add(new StageWindow(
                Stages[i].Stage,
                sowingDate.AddDays(offsets[i]),
                sowingDate.AddDays(Math.Max(endOffset, offsets[i]))));
        }

        return windows;
    }

    /// <summary>
    /// The stage a cycle should be in on <paramref name="date"/>: Nursery before sowing,
    /// Harvest from the expected harvest date onwards.
    /// </summary>
    public static GrowthStage ExpectedStageOn(DateOnly date, DateOnly sowingDate, int durationDays)
    {
        var offsets = BoundaryOffsets(durationDays);
        var dayOffset = date.DayNumber - sowingDate.DayNumber;

        if (dayOffset <= 0)
            return Stages[0].Stage;

        if (dayOffset >= durationDays)
            return Stages[^1].Stage;

        // Walk backwards so a zero-length window (only possible for very short
        // durations) is skipped rather than reported.
        for (var i = Stages.Length - 1; i >= 0; i--)
        {
            if (dayOffset >= offsets[i])
                return Stages[i].Stage;
        }

        return Stages[0].Stage;
    }

    /// <summary>Day offsets from sowing at which each stage starts, plus the closing boundary.</summary>
    private static int[] BoundaryOffsets(int durationDays)
    {
        if (durationDays < 1)
            throw new ArgumentOutOfRangeException(nameof(durationDays), "Duration must be at least one day.");

        var offsets = new int[Stages.Length + 1];
        for (var i = 0; i < Stages.Length; i++)
            offsets[i] = (int)Math.Round(Stages[i].StartFraction * durationDays, MidpointRounding.AwayFromZero);

        offsets[^1] = durationDays;

        // Rounding must never push a stage past the one after it.
        for (var i = 1; i < offsets.Length; i++)
            offsets[i] = Math.Max(offsets[i], offsets[i - 1]);

        return offsets;
    }
}
