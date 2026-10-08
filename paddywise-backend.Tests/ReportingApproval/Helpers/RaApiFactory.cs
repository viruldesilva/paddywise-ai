using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PaddyWise.Api.Services.ReportingApproval.Agents;
using PaddyWise.Backend.Tests.FieldCultivation.Helpers;

namespace PaddyWise.Backend.Tests.ReportingApproval.Helpers;

/// <summary>
/// FieldCultivationApiFactory (unchanged) with one difference: its IValidationAgentService
/// mock is replaced by the real ValidationAgentService, so Component 4's second validation
/// pass runs end to end. Its LLM is the same fake the planning agent uses.
/// </summary>
public class RaApiFactory : FieldCultivationApiFactory
{
    public RaApiFactory(bool useFakeLlm = true) : base(useFakeLlm)
    {
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureTestServices(services =>
        {
            foreach (var descriptor in services.Where(d => d.ServiceType == typeof(IValidationAgentService)).ToList())
                services.Remove(descriptor);
            services.AddScoped<IValidationAgentService, ValidationAgentService>();
        });
    }
}
