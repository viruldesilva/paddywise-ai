using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PaddyWise.Api.Data;
using PaddyWise.Api.DTOs.Shared;
using PaddyWise.Api.Entities.Shared;
using Xunit;

namespace PaddyWise.Backend.Tests;

[Trait("Component", "ReportingApproval")]
public class OfficerApprovalWorkflowE2ETests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public OfficerApprovalWorkflowE2ETests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CompleteOfficerApprovalWorkflow_E2E_SucceedsAcrossAllStages()
    {
        // ---------------------------------------------------------------------
        // STEP 0: Seed Admin user in database for approval action
        // ---------------------------------------------------------------------
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (!await context.Users.AnyAsync(u => u.Email == "admin@paddywise.lk"))
            {
                context.Users.Add(new User
                {
                    Name = "System Admin",
                    Email = "admin@paddywise.lk",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("AdminPassword123!"),
                    Role = UserRole.Admin,
                    AccountStatus = AccountStatus.Approved
                });
                await context.SaveChangesAsync();
            }
        }

        var client = _factory.CreateClient();
        var officerEmail = $"officer.{Guid.NewGuid():N}@agri.gov.lk";
        var officerPassword = "OfficerSecurePassword123!";

        // ---------------------------------------------------------------------
        // STEP 1: Officer Registers -> sets PendingApproval, returns no tokens
        // ---------------------------------------------------------------------
        var registerDto = new RegisterRequestDto
        {
            Name = "Officer Kasun",
            Email = officerEmail,
            Password = officerPassword,
            Role = "AgriculturalOfficer",
            Phone = "+94771122334"
        };

        var regResponse = await client.PostAsJsonAsync("/api/auth/register", registerDto);
        Assert.Equal(HttpStatusCode.OK, regResponse.StatusCode);

        var regContent = await regResponse.Content.ReadFromJsonAsync<RegisterResponseDto>();
        Assert.NotNull(regContent);
        Assert.True(regContent.RequiresApproval);
        Assert.Null(regContent.AccessToken);
        Assert.Null(regContent.RefreshToken);
        Assert.Contains("pending admin verification", regContent.Message);

        // ---------------------------------------------------------------------
        // STEP 2: Pending Officer attempts login -> blocked with 403 Forbidden
        // ---------------------------------------------------------------------
        var loginDto = new LoginRequestDto
        {
            Email = officerEmail,
            Password = officerPassword
        };

        var blockedLoginResponse = await client.PostAsJsonAsync("/api/auth/login", loginDto);
        Assert.Equal(HttpStatusCode.Forbidden, blockedLoginResponse.StatusCode);

        // ---------------------------------------------------------------------
        // STEP 3: Admin logs in -> gets Admin JWT token
        // ---------------------------------------------------------------------
        var adminLoginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = "admin@paddywise.lk",
            Password = "AdminPassword123!"
        });
        Assert.Equal(HttpStatusCode.OK, adminLoginResponse.StatusCode);

        var adminAuth = await adminLoginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(adminAuth);
        Assert.False(string.IsNullOrWhiteSpace(adminAuth.AccessToken));

        // ---------------------------------------------------------------------
        // STEP 4: Admin views pending requests -> finds new officer
        // ---------------------------------------------------------------------
        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.AccessToken);

        var requestsResponse = await adminClient.GetAsync("/api/admin/officer-requests");
        Assert.Equal(HttpStatusCode.OK, requestsResponse.StatusCode);

        var requests = await requestsResponse.Content.ReadFromJsonAsync<List<OfficerRequestDto>>();
        Assert.NotNull(requests);
        var pendingOfficer = requests.FirstOrDefault(r => r.Email == officerEmail);
        Assert.NotNull(pendingOfficer);
        Assert.Equal("Officer Kasun", pendingOfficer.Name);

        // ---------------------------------------------------------------------
        // STEP 5: Admin Approves the officer application
        // ---------------------------------------------------------------------
        var approveResponse = await adminClient.PostAsync($"/api/admin/officer-requests/{pendingOfficer.Id}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        // ---------------------------------------------------------------------
        // STEP 6: Approved Officer logs in successfully -> tokens issued
        // ---------------------------------------------------------------------
        var approvedLoginResponse = await client.PostAsJsonAsync("/api/auth/login", loginDto);
        Assert.Equal(HttpStatusCode.OK, approvedLoginResponse.StatusCode);

        var officerAuth = await approvedLoginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(officerAuth);
        Assert.False(string.IsNullOrWhiteSpace(officerAuth.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(officerAuth.RefreshToken));
        Assert.Equal("AgriculturalOfficer", officerAuth.Role);

        // ---------------------------------------------------------------------
        // STEP 7: Officer accesses protected officer-only endpoint with their token
        // ---------------------------------------------------------------------
        var officerClient = _factory.CreateClient();
        officerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", officerAuth.AccessToken);

        var reviewsResponse = await officerClient.GetAsync("/api/reviews/plans/1/draft-revision-comment");
        Assert.Equal(HttpStatusCode.OK, reviewsResponse.StatusCode);
    }
}
