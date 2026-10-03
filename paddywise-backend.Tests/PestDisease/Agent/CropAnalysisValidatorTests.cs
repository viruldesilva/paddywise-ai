using PaddyWise.Api.DTOs.PestDisease;
using PaddyWise.Api.Services.PestDisease;
using Xunit;

namespace PaddyWise.Backend.Tests.PestDisease.Agent;

/// <summary>
/// CropAnalysisValidator is a pure static function — no agent, no mock LLM, no database
/// needed to test it directly.
/// </summary>
[Trait("Component", "PestDisease")]
public class CropAnalysisValidatorTests
{
    [Theory]
    [InlineData(0.0, true)]
    [InlineData(1.0, true)]
    [InlineData(1.01, false)]
    [InlineData(-0.01, false)]
    public void AG05_ConfidenceBoundary_0And1Accepted_OutsideRangeRejected(double confidence, bool expectedValid)
    {
        var output = new CropAnalysisAgentOutput
        {
            PossibleIssues = new List<PossibleIssueCandidate>
            {
                new()
                {
                    Name = "Rice Blast",
                    Confidence = (decimal)confidence,
                    Source = "Sri Lanka Department of Agriculture"
                }
            },
            RecommendedNextStep = "Officer review recommended"
        };

        // "Rice Blast" was confirmed found via get_pest_knowledge during the run — only
        // confidence is the variable under test here.
        var knowledgeLookups = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            ["Rice Blast"] = true
        };

        var result = CropAnalysisValidator.Validate(output, knowledgeLookups);

        Assert.Equal(expectedValid, result.IsValid);
        if (!expectedValid)
        {
            Assert.Contains(result.Errors, e => e.Contains("outside the 0.00-1.00 range"));
        }
    }
}
