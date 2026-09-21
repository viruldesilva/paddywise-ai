using PaddyWise.Api.DTOs.PestDisease;

namespace PaddyWise.Api.Services.PestDisease;

/// <summary>The outcome of validating one diagnosis: valid, or every reason it is not.</summary>
public class CropAnalysisValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// The deterministic gate between the Crop Analysis Agent and the officer's queue. Plain code
/// on purpose — no database, no LLM — so a diagnosis is judged the same way every time.
/// Modelled on Services/FieldCultivation/CultivationPlanValidator.cs.
/// </summary>
public static class CropAnalysisValidator
{
    /// <summary>
    /// <paramref name="knowledgeLookups"/> is every pest/disease name the agent actually
    /// looked up via its get_pest_knowledge tool during this run, mapped to whether that
    /// lookup found a real PestDiseaseKnowledge entry. A candidate whose name is missing from
    /// this map, or mapped to false, was never confirmed — and is rejected rather than trusted
    /// on the model's word.
    /// </summary>
    public static CropAnalysisValidationResult Validate(
        CropAnalysisAgentOutput? output,
        IReadOnlyDictionary<string, bool> knowledgeLookups)
    {
        var errors = new List<string>();

        if (output == null)
        {
            errors.Add("The diagnosis agent returned no result.");
            return Failed(errors);
        }

        if (string.IsNullOrWhiteSpace(output.RecommendedNextStep))
            errors.Add("The result has no recommended next step.");

        for (var i = 0; i < output.PossibleIssues.Count; i++)
        {
            var candidate = output.PossibleIssues[i];
            var number = i + 1;

            if (string.IsNullOrWhiteSpace(candidate.Name))
            {
                errors.Add($"Possible issue {number} has no name.");
                continue;
            }

            if (candidate.Confidence < 0m || candidate.Confidence > 1m)
            {
                errors.Add(
                    $"Possible issue {number} ('{candidate.Name}') has confidence " +
                    $"{candidate.Confidence}, outside the 0.00-1.00 range.");
            }

            if (!knowledgeLookups.TryGetValue(candidate.Name.Trim(), out var found) || !found)
            {
                errors.Add(
                    $"Possible issue {number} names '{candidate.Name}', which was never " +
                    "confirmed against the PestDiseaseKnowledge base via get_pest_knowledge.");
            }

            if (string.IsNullOrWhiteSpace(candidate.Source))
                errors.Add($"Possible issue {number} ('{candidate.Name}') has no source.");
        }

        return errors.Count == 0
            ? new CropAnalysisValidationResult { IsValid = true }
            : Failed(errors);
    }

    private static CropAnalysisValidationResult Failed(List<string> errors) =>
        new() { IsValid = false, Errors = errors };
}
