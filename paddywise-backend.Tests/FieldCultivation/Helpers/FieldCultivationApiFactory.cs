using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PaddyWise.Api.Agents.Shared;
using PaddyWise.Api.Data;
using PaddyWise.Api.Services.ReportingApproval.Agents;
using PaddyWise.Backend.Tests.PestDisease.Helpers;

namespace PaddyWise.Backend.Tests.FieldCultivation.Helpers;

/// <summary>
/// WebApplicationFactory for the Component 1 API tests: the real Program.cs pipeline
/// (routing, [Authorize(Roles=...)], model binding, the controllers' own try/catch) against an
/// InMemory database, with PestDisease's TestAuthHandler for header-driven authentication.
///
/// It goes further than PestDiseaseApiFactory in three ways the plan endpoints need:
///  - the default ILlmClient is replaced by <see cref="Llm"/>, so a plan request never reaches
///    Gemini (pass useFakeLlm: false to keep the real registration, for the DI test);
///  - Component 4's IValidationAgentService is replaced by <see cref="ValidationAgent"/>, a
///    mock — its second pass is not under test here, only whether it is called;
///  - InMemory's TransactionIgnoredWarning is ignored, because approving a plan opens a
///    transaction.
/// </summary>
public class FieldCultivationApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "FieldCultivationApiTestDb_" + Guid.NewGuid();
    private readonly bool _useFakeLlm;

    public FieldCultivationApiFactory(bool useFakeLlm = true)
    {
        _useFakeLlm = useFakeLlm;
        ValidationAgent
            .Setup(v => v.ValidateCultivationPlanAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentValidationResult { IsValid = true });
    }

    /// <summary>The planning agent's LLM. Tests script it with <see cref="UseLlm"/>.</summary>
    public Mock<ILlmClient> Llm { get; private set; } = new();

    public List<FakePlanningLlmClient.CapturedCall> CapturedCalls { get; } = new();

    public Mock<IValidationAgentService> ValidationAgent { get; } = new();

    /// <summary>Scripts the fake LLM. Call before the first request is sent.</summary>
    public void UseLlm(FakePlanningLlmClient.OnCall onCall) =>
        Llm = FakePlanningLlmClient.Create(onCall, CapturedCalls);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:Key", "super-secret-paddywise-jwt-key-for-api-testing-32chars" },
                { "Jwt:Issuer", "PaddyWiseApi" },
                { "Jwt:Audience", "PaddyWiseClient" },
                { "Jwt:AccessTokenExpiryMinutes", "20" },
                { "Jwt:RefreshTokenExpiryDays", "7" },
                { "ConnectionStrings:DefaultConnection", "Host=localhost;Database=unused;Username=unused;Password=unused" },
                { "Gemini:ApiKey", "unused-in-tests" },
                { "Resend:ApiKey", "re_test_key" },
                { "Resend:FromEmail", "onboarding@resend.dev" },
                { "Resend:FromName", "PaddyWise AI" }
            });
        });

        builder.ConfigureTestServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbContextDescriptor != null)
                services.Remove(dbContextDescriptor);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_dbName)
                    .ConfigureWarnings(w => w.Ignore(
                        Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)));

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            if (_useFakeLlm)
            {
                // Only the unkeyed client: Component 3's keyed client is not this suite's business.
                var llm = services.Where(d => d.ServiceType == typeof(ILlmClient) && !d.IsKeyedService).ToList();
                foreach (var descriptor in llm)
                    services.Remove(descriptor);
                services.AddScoped(_ => Llm.Object);
            }

            services.RemoveAll<IValidationAgentService>();
            services.AddScoped(_ => ValidationAgent.Object);
        });
    }

    public ApplicationDbContext CreateDbContext() => new(FcTestDb.Options(_dbName));

    /// <summary>An HttpClient acting as the given user and role through TestAuthHandler's headers.</summary>
    public HttpClient CreateClientAs(int userId, string role)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);
        return client;
    }
}

internal static class ServiceCollectionTestExtensions
{
    public static void RemoveAll<T>(this IServiceCollection services)
    {
        foreach (var descriptor in services.Where(d => d.ServiceType == typeof(T)).ToList())
            services.Remove(descriptor);
    }
}
