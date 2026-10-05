using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaddyWise.Api.Data;

namespace PaddyWise.Backend.Tests.PestDisease.Helpers;

/// <summary>
/// WebApplicationFactory for the PestDisease API tests. Each test creates its own instance
/// (own InMemory database, matching the per-test fresh-database convention the rest of this
/// project already uses) rather than sharing one via IClassFixture.
///
/// Swaps the real Postgres ApplicationDbContext for an InMemory one, and the real JWT bearer
/// authentication for TestAuthHandler, so a test can act as any user via CreateClientAs
/// without needing a real signed token. Program.cs's own startup guards (Jwt:Key,
/// ConnectionStrings:DefaultConnection must be non-empty) are satisfied with placeholder
/// values — the connection string is never actually used once the DbContext is swapped.
/// </summary>
public class PestDiseaseApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "PestDiseaseApiTestDb_" + Guid.NewGuid();

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
                { "Brevo:ApiKey", "xkeysib_test_key" },
                { "Brevo:FromEmail", "onboarding@paddywise.ai" },
                { "Brevo:FromName", "PaddyWise AI" }
            });
        });

        builder.ConfigureTestServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbContextDescriptor != null)
                services.Remove(dbContextDescriptor);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    /// <summary>A fresh ApplicationDbContext on this factory's own InMemory database — use
    /// this to seed data a test then exercises through an HttpClient from this same factory.</summary>
    public ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(_dbName)
            .Options;

        return new ApplicationDbContext(options);
    }

    /// <summary>An HttpClient that authenticates as the given user/role via TestAuthHandler's
    /// headers — not a real token, but exercises [Authorize]/[Authorize(Roles=...)] and
    /// ClaimTypes.NameIdentifier the same way a real one would.</summary>
    public HttpClient CreateClientAs(int userId, string role)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);
        return client;
    }
}
