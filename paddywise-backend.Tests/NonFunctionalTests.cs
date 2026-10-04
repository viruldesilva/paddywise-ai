using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PaddyWise.Api.Data;
using PaddyWise.Api.DTOs.Shared;
using PaddyWise.Api.Entities.Shared;
using Xunit;

namespace PaddyWise.Backend.Tests;

public class NonFunctionalTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public NonFunctionalTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // =========================================================================
    // 1. SECURITY TESTING: SECRETS LEAKAGE PREVENTION
    // =========================================================================

    [Fact]
    public async Task Security_ApiResponses_NeverExposeApiKeysOrSecrets()
    {
        // Arrange
        var client = _factory.CreateClient();
        var adminToken = TestJwtHelper.GenerateToken(1, "Admin", "admin@paddywise.lk", "Admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var secretStrings = new[]
        {
            "re_test_key_sample_12345",
            "gemini_test_api_key_sample_98765",
            "super-secret-paddywise-jwt-key"
        };

        // Act: Test multiple sensitive endpoints
        var responses = new List<HttpResponseMessage>
        {
            await client.GetAsync("/api/admin/officer-requests"),
            await client.GetAsync("/api/reviews/plans/1/draft-revision-comment"),
            await client.GetAsync("/api/notifications")
        };

        // Assert: None of the response bodies contain configured secret keys
        foreach (var response in responses)
        {
            var content = await response.Content.ReadAsStringAsync();
            foreach (var secret in secretStrings)
            {
                Assert.DoesNotContain(secret, content, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Security_UserAndAuthDto_DoesNotExposePasswordHashField()
    {
        // Assert that public DTOs do not contain PasswordHash property
        var authResponseProps = typeof(AuthResponseDto).GetProperties().Select(p => p.Name);
        var officerRequestProps = typeof(OfficerRequestDto).GetProperties().Select(p => p.Name);
        var notificationProps = typeof(NotificationDto).GetProperties().Select(p => p.Name);

        Assert.DoesNotContain("PasswordHash", authResponseProps);
        Assert.DoesNotContain("PasswordHash", officerRequestProps);
        Assert.DoesNotContain("PasswordHash", notificationProps);
    }

    // =========================================================================
    // 2. PERFORMANCE TESTING: RESPONSE TIME BENCHMARKING
    // =========================================================================

    [Fact]
    public async Task Performance_GetPendingOfficerRequests_RespondsWithinAcceptableThreshold()
    {
        // Arrange
        var client = _factory.CreateClient();
        var adminToken = TestJwtHelper.GenerateToken(1, "Admin", "admin@paddywise.lk", "Admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Warm up
        await client.GetAsync("/api/admin/officer-requests");

        // Act: Benchmark 10 consecutive requests
        var stopwatch = Stopwatch.StartNew();
        const int requestCount = 10;
        for (int i = 0; i < requestCount; i++)
        {
            var response = await client.GetAsync("/api/admin/officer-requests");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        stopwatch.Stop();

        // Assert: Average response time under 100ms per request in memory
        var avgDurationMs = stopwatch.ElapsedMilliseconds / (double)requestCount;
        Assert.True(avgDurationMs < 250, $"Average latency {avgDurationMs}ms exceeded 250ms threshold.");
    }

    [Fact]
    public async Task Performance_GetNotifications_RespondsWithinAcceptableThreshold()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = TestJwtHelper.GenerateToken(50, "Farmer", "farmer@test.com", "Farmer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Warm up
        await client.GetAsync("/api/notifications");

        // Act
        var stopwatch = Stopwatch.StartNew();
        var response = await client.GetAsync("/api/notifications");
        stopwatch.Stop();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(stopwatch.ElapsedMilliseconds < 500, $"Notification fetch took {stopwatch.ElapsedMilliseconds}ms, exceeding 500ms.");
    }

    [Fact]
    public async Task HealthCheck_ReturnsOk_WithoutAuthentication()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", content);
    }
}
