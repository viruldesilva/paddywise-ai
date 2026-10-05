using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PaddyWise.Api.Data;

namespace PaddyWise.Backend.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "IntegrationTestDb_" + Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=dummy;Username=postgres;Password=postgres",
                ["Jwt:Key"] = TestJwtHelper.TestJwtKey,
                ["Jwt:Issuer"] = TestJwtHelper.TestIssuer,
                ["Jwt:Audience"] = TestJwtHelper.TestAudience,
                ["Jwt:AccessTokenExpiryMinutes"] = "20",
                ["Jwt:RefreshTokenExpiryDays"] = "7",
                ["Brevo:ApiKey"] = "xkeysib_test_key_sample_12345",
                ["Brevo:FromEmail"] = "onboarding@paddywise.ai",
                ["Brevo:FromName"] = "PaddyWise AI",
                ["Gemini:ApiKey"] = "gemini_test_api_key_sample_98765"
            });
        });

        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });

            // Ensure JwtBearer middleware validates against TestJwtKey
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = TestJwtHelper.TestIssuer,
                    ValidAudience = TestJwtHelper.TestAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtHelper.TestJwtKey))
                };
            });
        });
    }
}
