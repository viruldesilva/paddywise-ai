using PaddyWise.Api.Agents.FieldCultivation;
using PaddyWise.Api.Agents.Shared;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Services.FieldCultivation;
using Xunit;

namespace PaddyWise.Backend.Tests.FieldCultivation.Agent;

/// <summary>
/// CultivationPlanValidator called directly — the deterministic gate every plan passes before
/// it reaches an officer. A fixed calendar (sown 2026-01-01, 120 days) is used so every
/// boundary is an exact, readable date; requestedOn is the sowing date unless a test is about
/// past windows.
/// </summary>
[Trait("Component", "FieldCultivation")]
public class CultivationPlanValidatorTests
{
    private static readonly DateOnly Sowing = new(2026, 1, 1);
    private static readonly DateOnly Harvest = Sowing.AddDays(120);
    private static readonly IReadOnlyList<StageWindow> Timeline = StageTimelineCalculator.Build(Sowing, 120);

    private static StageWindow WindowOf(GrowthStage stage) => Timeline.Single(w => w.Stage == stage);

    private static PlanStep Step(
        GrowthStage stage,
        DateOnly? start = null,
        DateOnly? end = null,
        string task = "Follow the ResourceAnalysisAgent recommendation.",
        string category = PlanStepCategories.Monitoring)
    {
        var window = WindowOf(stage);
        return new PlanStep
        {
            Stage = stage.ToString(),
            WindowStart = start ?? window.Start,
            WindowEnd = end ?? window.End,
            Task = task,
            Rationale = "Test.",
            Category = category
        };
    }

    private static CultivationPlanOutput Plan(params PlanStep[] steps) => new()
    {
        Summary = "A plan.",
        Steps = steps.ToList(),
        Delegations = new List<PlanDelegation>
        {
            new() { TargetAgent = AgentNames.SchedulingValidation, Instruction = "Validate the plan." }
        }
    };

    private static ValidationResult Validate(CultivationPlanOutput plan, DateOnly? requestedOn = null) =>
        CultivationPlanValidator.Validate(plan, Timeline, Sowing, Harvest, requestedOn ?? Sowing);

    [Fact]
    public void AG10_AValidPlan_PassesWithNoErrors()
    {
        var result = Validate(Plan(Step(GrowthStage.Nursery), Step(GrowthStage.Tillering), Step(GrowthStage.Harvest)));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void AG03_AStageMissingFromTheTimeline_IsRejected()
    {
        // A timeline without Flowering: a step naming it cannot have come from get_stage_timeline.
        var partial = Timeline.Where(w => w.Stage != GrowthStage.Flowering).ToList();
        var plan = Plan(Step(GrowthStage.Flowering));

        var result = CultivationPlanValidator.Validate(plan, partial, Sowing, Harvest, Sowing);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not in this cycle's stage timeline"));
    }

    [Fact]
    public void AG03b_AnUnknownStageName_IsRejected()
    {
        var step = Step(GrowthStage.Tillering) with { Stage = "Ripening" };

        var result = Validate(Plan(step));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("unknown growth stage 'Ripening'"));
    }

    [Theory]
    [InlineData(-3, 0, true)]   // starts exactly 3 days before the window
    [InlineData(0, 3, true)]    // ends exactly 3 days after the window
    [InlineData(-4, 0, false)]  // one day further out on the left
    [InlineData(0, 4, false)]   // one day further out on the right
    public void AG04_StageWindowTolerance_ThreeDaysEitherSide(int startShift, int endShift, bool expectedValid)
    {
        var panicle = WindowOf(GrowthStage.PanicleInitiation);
        var step = Step(GrowthStage.PanicleInitiation, panicle.Start.AddDays(startShift), panicle.End.AddDays(endShift));

        var result = Validate(Plan(step));

        Assert.Equal(expectedValid, result.IsValid);
        if (!expectedValid)
            Assert.Contains(result.Errors, e => e.Contains("±3 days allowed"));
    }

    [Theory]
    [InlineData(0, true)]   // window ends on the day the plan is requested
    [InlineData(-1, false)] // window ended the day before
    public void AG05_PastWindows_EndingBeforeTheRequestDateAreRejected(int endRelativeToRequest, bool expectedValid)
    {
        var tillering = WindowOf(GrowthStage.Tillering);
        var requestedOn = tillering.End.AddDays(-endRelativeToRequest);

        var result = Validate(Plan(Step(GrowthStage.Tillering)), requestedOn);

        Assert.Equal(expectedValid, result.IsValid);
        if (!expectedValid)
            Assert.Contains(result.Errors, e => e.Contains("is in the past"));
    }

    [Theory]
    [InlineData("start", 0, true)]   // starts on the sowing date
    [InlineData("start", -1, false)] // the day before sowing
    [InlineData("end", 0, true)]     // ends on the expected harvest date
    [InlineData("end", 1, false)]    // the day after expected harvest
    public void AG06_SeasonBounds_StepsMustStayInsideSowingToHarvest(string edge, int shift, bool expectedValid)
    {
        var step = edge == "start"
            ? Step(GrowthStage.Nursery, Sowing.AddDays(shift))
            : Step(GrowthStage.Harvest, end: Harvest.AddDays(shift));

        var result = Validate(Plan(step), Sowing.AddDays(-5));

        Assert.Equal(expectedValid, result.IsValid);
        if (!expectedValid)
            Assert.Contains(result.Errors, e => e.Contains("outside the cycle's calendar"));
    }

    [Theory]
    [InlineData("Apply 50 kg of urea per acre.", false)]
    [InlineData("Apply 50kg urea.", false)]
    [InlineData("Spread 2 bags of TSP.", false)]
    [InlineData("Use 20% of the basal rate.", false)]
    [InlineData("Spray 1.5 l of solution.", false)]
    [InlineData("Mix 250 ml into the tank.", false)]
    [InlineData("Top-dress following the ResourceAnalysisAgent recommendation.", true)]
    [InlineData("Quantities in kg come from the ResourceAnalysisAgent.", true)]
    public void AG07_QuantitiesInTaskText_AreOutOfScope(string task, bool expectedValid)
    {
        var step = Step(GrowthStage.Tillering, task: task, category: PlanStepCategories.Nutrient);

        var result = Validate(Plan(step));

        Assert.Equal(expectedValid, result.IsValid);
        if (!expectedValid)
            Assert.Contains(result.Errors, e => e.Contains("dosage decision out of scope"));
    }

    [Theory]
    [InlineData(AgentNames.ResourceAnalysis, "Decide the top-dressing quantity.", true)]
    [InlineData(AgentNames.PestDiseaseDiagnosis, "Set a monitoring schedule.", true)]
    [InlineData(AgentNames.SchedulingValidation, "Validate the plan.", true)]
    [InlineData(AgentNames.CultivationPlanning, "Plan again.", false)] // cannot delegate to itself
    [InlineData("WeatherAgent", "Get the forecast.", false)]
    [InlineData("resourceanalysisagent", "Wrong case.", false)]         // ordinal match only
    [InlineData(AgentNames.ResourceAnalysis, "   ", false)]             // no instruction
    public void AG08_DelegationTargets_OnlyTheThreeOtherAgentsWithAnInstruction(
        string target, string instruction, bool expectedValid)
    {
        var plan = Plan(Step(GrowthStage.Tillering)) with
        {
            Delegations = new List<PlanDelegation> { new() { TargetAgent = target, Instruction = instruction } }
        };

        var result = Validate(plan);

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Fact]
    public void AG09a_EmptySummaryAndNoSteps_AreBothReported()
    {
        var plan = new CultivationPlanOutput { Summary = "  " };

        var result = Validate(plan);

        Assert.False(result.IsValid);
        Assert.Contains("The plan has no summary.", result.Errors);
        Assert.Contains("The plan contains no steps.", result.Errors);
    }

    [Fact]
    public void AG09b_AnUnknownCategory_IsRejected()
    {
        var result = Validate(Plan(Step(GrowthStage.Tillering, category: "Fertiliser")));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("unknown category 'Fertiliser'"));
    }

    [Fact]
    public void AG09c_ABackwardsWindow_IsReportedOnceAndSkipsTheOtherDateChecks()
    {
        var tillering = WindowOf(GrowthStage.Tillering);
        // Backwards AND before sowing: only the backwards error may appear for this step.
        var step = Step(GrowthStage.Tillering, tillering.End, Sowing.AddDays(-10));

        var result = Validate(Plan(step));

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("before it starts", result.Errors[0]);
    }

    [Fact]
    public void AG09d_StepsOutOfChronologicalOrder_AreRejected()
    {
        var result = Validate(Plan(Step(GrowthStage.Flowering), Step(GrowthStage.Tillering)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("chronological order"));
    }

    [Fact]
    public void AG09e_EveryProblemIsReported_NotJustTheFirst()
    {
        var bad = Step(GrowthStage.Tillering, task: "Apply 50 kg urea.", category: "Fertiliser");
        var plan = Plan(bad) with
        {
            Summary = "",
            Delegations = new List<PlanDelegation> { new() { TargetAgent = "Nobody", Instruction = "" } }
        };

        var result = Validate(plan);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 5, string.Join(" | ", result.Errors));
    }
}
