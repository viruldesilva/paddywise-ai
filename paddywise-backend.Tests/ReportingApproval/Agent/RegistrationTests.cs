using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PaddyWise.Api.Agents.Shared;
using PaddyWise.Api.Services.ReportingApproval;
using PaddyWise.Api.Services.ReportingApproval.Agents;
using PaddyWise.Backend.Tests.ReportingApproval.Helpers;
using Xunit;

namespace PaddyWise.Backend.Tests.ReportingApproval.Agent;

/// <summary>The app's real DI (Program.cs) for Component 4's three LLM-backed services.</summary>
[Trait("Component", "ReportingApproval")]
public class RegistrationTests
{
    private static object? LlmOf(object service) =>
        service.GetType().GetField("_llm", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(service);

    [Theory]
    [InlineData(typeof(IValidationAgentService), typeof(ValidationAgentService))]
    [InlineData(typeof(IRevisionDraftService), typeof(RevisionDraftService))]
    [InlineData(typeof(INotificationMessageService), typeof(NotificationMessageService))]
    public void RAAG18_EachService_ResolvesToItsClass_WithTheScopesLlmClient(Type contract, Type implementation)
    {
        // useFakeLlm: false keeps the real GeminiLlmClient registration.
        using var factory = new RaApiFactory(useFakeLlm: false);
        using var scope = factory.Services.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService(contract);

        Assert.IsType(implementation, service);
        var llm = scope.ServiceProvider.GetRequiredService<ILlmClient>();
        Assert.IsType<GeminiLlmClient>(llm);
        Assert.Same(llm, LlmOf(service));
    }
}
