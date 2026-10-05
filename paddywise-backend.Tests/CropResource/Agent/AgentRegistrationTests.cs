using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PaddyWise.Api.Agents.CropResource;
using PaddyWise.Api.Agents.Shared;
using PaddyWise.Backend.Tests.FieldCultivation.Helpers;
using Xunit;

namespace PaddyWise.Backend.Tests.CropResource.Agent;

/// <summary>
/// The app's real DI setup (Program.cs): Component 2's service interface and its
/// delegation key both resolve to ResourceAnalysisAgent, with the scope's default ILlmClient.
/// Uses FieldCultivationApiFactory(useFakeLlm: false) so the real registrations are kept.
/// </summary>
[Trait("Component", "CropResource")]
public class AgentRegistrationTests
{
    private static object? LlmOf(object agent) =>
        typeof(ResourceAnalysisAgent)
            .GetField("_llmClient", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(agent);

    [Fact]
    public void AG21a_TheAnalysisService_ResolvesToResourceAnalysisAgent_WithTheScopesLlmClient()
    {
        using var factory = new FieldCultivationApiFactory(useFakeLlm: false);
        using var scope = factory.Services.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<ICropActivityAnalysisService>();

        Assert.IsType<ResourceAnalysisAgent>(service);
        var expected = scope.ServiceProvider.GetRequiredService<ILlmClient>();
        Assert.IsType<GeminiLlmClient>(expected);
        Assert.Same(expected, LlmOf(service));
    }

    [Fact]
    public void AG21b_TheResourceAnalysisDelegationKey_ResolvesToResourceAnalysisAgent()
    {
        using var factory = new FieldCultivationApiFactory(useFakeLlm: false);
        using var scope = factory.Services.CreateScope();

        var agent = scope.ServiceProvider.GetRequiredKeyedService<IAgent<DelegatedTask, DelegatedTaskResult>>(
            AgentNames.ResourceAnalysis);

        var typed = Assert.IsType<ResourceAnalysisAgent>(agent);
        Assert.Equal(AgentNames.ResourceAnalysis, typed.Name);
        Assert.Same(scope.ServiceProvider.GetRequiredService<ILlmClient>(), LlmOf(agent));
    }
}
