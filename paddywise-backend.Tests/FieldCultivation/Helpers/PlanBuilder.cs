using System.Text.Json;
using PaddyWise.Api.Agents.FieldCultivation;
using PaddyWise.Api.Agents.Shared;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Services.FieldCultivation;

namespace PaddyWise.Backend.Tests.FieldCultivation.Helpers;

/// <summary>
/// Builds plans whose dates are copied from the cycle's real stage timeline — what a
/// well-behaved model produces after calling get_stage_timeline — so each test only changes
/// the one thing it is about.
/// </summary>
public static class PlanBuilder
{
    public static IReadOnlyList<StageWindow> Timeline(CultivationCycle cycle) =>
        StageTimelineCalculator.Build(
            cycle.SowingDate, cycle.ExpectedHarvestDate.DayNumber - cycle.SowingDate.DayNumber);

    public static StageWindow Window(CultivationCycle cycle, GrowthStage stage) =>
        Timeline(cycle).Single(w => w.Stage == stage);

    /// <summary>
    /// A plan that passes CultivationPlanValidator for a cycle sown 20 days ago: everything
    /// from today forward, no quantities, a delegation to SchedulingValidationAgent.
    /// </summary>
    public static CultivationPlanOutput Valid(CultivationCycle cycle, string nutrientTask =
        "Top-dress the field following the ResourceAnalysisAgent recommendation.")
    {
        var today = FcTestDb.Today;
        var tillering = Window(cycle, GrowthStage.Tillering);
        var panicle = Window(cycle, GrowthStage.PanicleInitiation);
        var harvest = Window(cycle, GrowthStage.Harvest);

        return new CultivationPlanOutput
        {
            Summary = "Tillering is under way; this plan covers the rest of the season.",
            Steps = new List<PlanStep>
            {
                new()
                {
                    Stage = "Tillering",
                    WindowStart = today > tillering.Start ? today : tillering.Start,
                    WindowEnd = tillering.End,
                    Task = nutrientTask,
                    Rationale = "Tillering needs nitrogen.",
                    Category = PlanStepCategories.Nutrient
                },
                new()
                {
                    Stage = "PanicleInitiation",
                    WindowStart = panicle.Start,
                    WindowEnd = panicle.End,
                    Task = "Keep standing water in the field.",
                    Rationale = "Panicle initiation is water-sensitive.",
                    Category = PlanStepCategories.Water
                },
                new()
                {
                    Stage = "Harvest",
                    WindowStart = harvest.Start,
                    WindowEnd = harvest.End,
                    Task = "Harvest when most grains are golden.",
                    Rationale = "Avoid shattering losses.",
                    Category = PlanStepCategories.Harvest
                }
            },
            Delegations = new List<PlanDelegation>
            {
                new()
                {
                    TargetAgent = AgentNames.SchedulingValidation,
                    Instruction = "Validate the whole plan.",
                    Payload = new Dictionary<string, object> { ["cycleId"] = cycle.Id }
                }
            },
            Assumptions = new List<string> { "Canal water is available all season." }
        };
    }

    public static string Json(CultivationPlanOutput plan) =>
        JsonSerializer.Serialize(plan, CultivationPlanJson.Options);
}
