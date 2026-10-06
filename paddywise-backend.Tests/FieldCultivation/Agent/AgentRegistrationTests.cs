using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PaddyWise.Api.Agents.FieldCultivation;
using PaddyWise.Api.Agents.Shared;
using PaddyWise.Backend.Tests.FieldCultivation.Helpers;
using Xunit;

namespace PaddyWise.Backend.Tests.FieldCultivation.Agent;

/// <summary>
/// The app's real DI setup (Program.cs): the planning agent resolves to
/// CultivationPlanningAgent with the scope's default ILlmClient, and every agent the plan may
/// delegate to is registered under its AgentNames key — dispatch resolves them by that key.
/// </summary>
[Trait("Component", "FieldCultivation")]
public class AgentRegistrationTests
{
    [Fact]
    public void AG20a_PlanningAgent_ResolvesToCultivationPlanningAgent_WithTheScopesLlmClient()
    {
        using var factory = new FieldCultivationApiFactory(useFakeLlm: false);
        using var scope = factory.Services.CreateScope();

        var agent = scope.ServiceProvider.GetRequiredService<IAgent<PlanAgentInput, CultivationPlanOutput>>();

        var planning = Assert.IsType<CultivationPlanningAgent>(agent);
        Assert.Equal(AgentNames.CultivationPlanning, planning.Name);

        var expectedLlm = scope.ServiceProvider.GetRequiredService<ILlmClient>();
        Assert.IsType<GeminiLlmClient>(expectedLlm);

        var field = typeof(CultivationPlanningAgent).GetField("_llm", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        Assert.Same(expectedLlm, field!.GetValue(agent));
    }

    [Theory]
    [InlineData(AgentNames.ResourceAnalysis)]
    [InlineData(AgentNames.PestDiseaseDiagnosis)]
    [InlineData(AgentNames.SchedulingValidation)]
    public void AG20b_EveryDelegationTarget_IsRegisteredUnderItsKey(string key)
    {
        using var factory = new FieldCultivationApiFactory(useFakeLlm: false);
        using var scope = factory.Services.CreateScope();

        var agent = scope.ServiceProvider.GetKeyedService<IAgent<DelegatedTask, DelegatedTaskResult>>(key);

        Assert.NotNull(agent);
        Assert.Equal(key, agent!.Name);
    }

    [Fact]
    public void AG20c_ThePlanningAgent_IsNotADelegationTarget()
    {
        using var factory = new FieldCultivationApiFactory(useFakeLlm: false);
        using var scope = factory.Services.CreateScope();

        Assert.Null(scope.ServiceProvider.GetKeyedService<IAgent<DelegatedTask, DelegatedTaskResult>>(
            AgentNames.CultivationPlanning));
    }
}
