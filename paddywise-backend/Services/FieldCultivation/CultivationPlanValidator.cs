using System.Text.RegularExpressions;
using PaddyWise.Api.Agents.FieldCultivation;
using PaddyWise.Api.Agents.Shared;
using PaddyWise.Api.Entities.FieldCultivation;

namespace PaddyWise.Api.Services.FieldCultivation;

/// <summary>The outcome of validating one plan: valid, or every reason it is not.</summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// The deterministic gate between the planning agent and the officer's queue. It is plain
/// code on purpose — no database, no LLM — so a plan is judged the same way every time and
/// the reasons it failed can be shown to the farmer verbatim.
/// </summary>
public static class CultivationPlanValidator
{
    /// <summary>
    /// How far outside its stage's window a step may sit. The stage boundaries are computed
    /// from fractions of the season, so a step a couple of days either side of one is an
    /// agronomic judgement call rather than a mistake.
    /// </summary>
    private const int StageWindowToleranceDays = 3;

    /// <summary>
    /// Quantities belong to the Resource Analysis agent. A number followed by a unit in a
    /// step's task text means the planning agent decided a dosage it has no authority over.
    /// Percent is its own branch: \b after a non-word character only matches when a word
    /// character follows, so "20% of the basal rate" would slip past a trailing boundary.
    /// </summary>
    private static readonly Regex DosagePattern = new(
        @"\d+(\.\d+)?\s?(kg|g|ml|l|litre|liter|bag|bags)\b|\d+(\.\d+)?\s?%",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>The agents a delegation may target — the planning agent cannot delegate to itself.</summary>
    private static readonly string[] DelegationTargets =
    {
        AgentNames.ResourceAnalysis,
        AgentNames.PestDiseaseDiagnosis,
        AgentNames.SchedulingValidation
    };

    public static ValidationResult Validate(
        CultivationPlanOutput plan,
        IReadOnlyList<StageWindow> timeline,
        DateOnly sowingDate,
        DateOnly expectedHarvest,
        DateOnly requestedOn)
    {
        var errors = new List<string>();

        if (plan == null)
        {
            errors.Add("The planning agent returned no plan.");
            return Failed(errors);
        }

        if (string.IsNullOrWhiteSpace(plan.Summary))
            errors.Add("The plan has no summary.");

        if (plan.Steps.Count == 0)
            errors.Add("The plan contains no steps.");

        ValidateSteps(plan, timeline, sowingDate, expectedHarvest, requestedOn, errors);
        ValidateDelegations(plan, errors);

        return errors.Count == 0
            ? new ValidationResult { IsValid = true }
            : Failed(errors);
    }

    private static void ValidateSteps(
        CultivationPlanOutput plan,
        IReadOnlyList<StageWindow> timeline,
        DateOnly sowingDate,
        DateOnly expectedHarvest,
        DateOnly requestedOn,
        List<string> errors)
    {
        PlanStep? previous = null;
        var previousNumber = 0;

        for (var i = 0; i < plan.Steps.Count; i++)
        {
            var step = plan.Steps[i];
            var number = i + 1;

            var stageParsed = Enum.TryParse<GrowthStage>(step.Stage, true, out var stage) && Enum.IsDefined(stage);

            if (!stageParsed)
            {
                errors.Add($"Step {number} names an unknown growth stage '{Describe(step.Stage)}'.");
            }

            if (!PlanStepCategories.All.Contains(step.Category, StringComparer.Ordinal))
            {
                errors.Add(
                    $"Step {number} uses an unknown category '{Describe(step.Category)}' — " +
                    $"it must be one of {string.Join(", ", PlanStepCategories.All)}.");
            }

            // Every date rule below reads both ends of the window, so a backwards window is
            // reported once and the rest of this step's window checks are skipped.
            if (step.WindowStart > step.WindowEnd)
            {
                errors.Add(
                    $"Step {number} ends on {step.WindowEnd:yyyy-MM-dd}, before it starts on {step.WindowStart:yyyy-MM-dd}.");
            }
            else
            {
                if (stageParsed)
                    ValidateAgainstStageWindow(step, stage, number, timeline, errors);

                if (step.WindowStart < sowingDate || step.WindowEnd > expectedHarvest)
                {
                    errors.Add(
                        $"Step {number} runs {step.WindowStart:yyyy-MM-dd} to {step.WindowEnd:yyyy-MM-dd}, " +
                        $"outside the cycle's calendar {sowingDate:yyyy-MM-dd} to {expectedHarvest:yyyy-MM-dd}.");
                }

                if (step.WindowEnd < requestedOn)
                {
                    errors.Add(
                        $"Step {number} is in the past: its window ended on {step.WindowEnd:yyyy-MM-dd}, " +
                        $"before the plan was requested on {requestedOn:yyyy-MM-dd}.");
                }
            }

            if (previous != null && step.WindowStart < previous.WindowStart)
            {
                errors.Add(
                    $"Step {number} starts on {step.WindowStart:yyyy-MM-dd}, before step {previousNumber} " +
                    $"which starts on {previous.WindowStart:yyyy-MM-dd}; steps must be in chronological order.");
            }

            var dosage = DosagePattern.Match(step.Task ?? string.Empty);
            if (dosage.Success)
            {
                errors.Add(
                    $"Step {number} states a quantity ('{dosage.Value.Trim()}') — dosage decision out of scope; " +
                    "quantities belong to the Resource Analysis agent.");
            }

            previous = step;
            previousNumber = number;
        }
    }

    private static void ValidateAgainstStageWindow(
        PlanStep step,
        GrowthStage stage,
        int number,
        IReadOnlyList<StageWindow> timeline,
        List<string> errors)
    {
        StageWindow? window = null;
        foreach (var candidate in timeline)
        {
            if (candidate.Stage == stage)
            {
                window = candidate;
                break;
            }
        }

        if (window == null)
        {
            errors.Add($"Step {number} names stage {stage}, which is not in this cycle's stage timeline.");
            return;
        }

        var earliest = window.Value.Start.AddDays(-StageWindowToleranceDays);
        var latest = window.Value.End.AddDays(StageWindowToleranceDays);

        if (step.WindowStart < earliest || step.WindowEnd > latest)
        {
            errors.Add(
                $"Step {number} runs {step.WindowStart:yyyy-MM-dd} to {step.WindowEnd:yyyy-MM-dd}, outside the " +
                $"{stage} stage window {window.Value.Start:yyyy-MM-dd} to {window.Value.End:yyyy-MM-dd} " +
                $"(±{StageWindowToleranceDays} days allowed).");
        }
    }

    private static void ValidateDelegations(CultivationPlanOutput plan, List<string> errors)
    {
        for (var i = 0; i < plan.Delegations.Count; i++)
        {
            var delegation = plan.Delegations[i];
            var number = i + 1;

            if (!DelegationTargets.Contains(delegation.TargetAgent, StringComparer.Ordinal))
            {
                errors.Add(
                    $"Delegation {number} targets unknown agent '{Describe(delegation.TargetAgent)}' — " +
                    $"it must be one of {string.Join(", ", DelegationTargets)}.");
            }

            if (string.IsNullOrWhiteSpace(delegation.Instruction))
                errors.Add($"Delegation {number} has no instruction for the agent it targets.");
        }
    }

    private static ValidationResult Failed(List<string> errors) =>
        new() { IsValid = false, Errors = errors };

    /// <summary>Keeps an empty or whitespace value readable inside an error message.</summary>
    private static string Describe(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "(empty)" : value.Trim();
}
