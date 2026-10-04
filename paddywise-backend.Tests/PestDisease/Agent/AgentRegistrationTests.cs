using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PaddyWise.Api.Agents.PestDisease;
using PaddyWise.Api.Agents.Shared;
using PaddyWise.Backend.Tests.PestDisease.Helpers;
using Xunit;

namespace PaddyWise.Backend.Tests.PestDisease.Agent;

/// <summary>
/// Confirms the app's real DI setup (Program.cs) wires AgentNames.PestDiseaseDiagnosis to
/// CropAnalysisAgent, and that the agent's own ILlmClient is the keyed pest-disease client
/// (not just any GeminiLlmClient) — reflection-reads the agent's private _llm field and
/// compares it by reference to the same scope's keyed resolution, rather than only checking
/// the type.
/// </summary>
[Trait("Component", "PestDisease")]
public class AgentRegistrationTests
{
    [Fact]
    public void AG14_KeyedPestDiseaseAgent_ResolvesToCropAnalysisAgent_WithTheKeyedLlmClient()
    {
        using var factory = new PestDiseaseApiFactory();
        using var scope = factory.Services.CreateScope();

        var agent = scope.ServiceProvider.GetRequiredKeyedService<IAgent<DelegatedTask, DelegatedTaskResult>>(
            AgentNames.PestDiseaseDiagnosis);

        Assert.IsType<CropAnalysisAgent>(agent);

        var expectedLlm = scope.ServiceProvider.GetRequiredKeyedService<ILlmClient>(AgentNames.PestDiseaseDiagnosis);

        var field = typeof(CropAnalysisAgent).GetField("_llm", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        var actualLlm = field!.GetValue(agent);

        Assert.Same(expectedLlm, actualLlm);
    }
}
