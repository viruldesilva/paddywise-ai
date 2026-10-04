using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PaddyWise.Api.Agents.CropResource;
using PaddyWise.Api.Data;
using PaddyWise.Api.Entities.CropResource;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.Shared;
using Xunit;

namespace PaddyWise.Backend.Tests;

public class CropActivityAnalysisControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CropActivityAnalysisControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(User farmer, User officer, CultivationCycle cycle, CropActivityRecommendation rec)> SeedRecommendationDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var suffix = Guid.NewGuid().ToString("N")[..6];
        var division = new Division
        {
            Name = $"Polonnaruwa_{suffix}",
            District = "Polonnaruwa",
            Province = "North Central"
        };
        context.Divisions.Add(division);

        var farmer = new User
        {
            Name = $"Farmer_{suffix}",
            Email = $"farmer_{suffix}@test.com",
            PasswordHash = "hash",
            Role = UserRole.Farmer,
            AccountStatus = AccountStatus.Approved
        };
        var officer = new User
        {
            Name = $"Officer_{suffix}",
            Email = $"officer_{suffix}@gov.lk",
            PasswordHash = "hash",
            Role = UserRole.AgriculturalOfficer,
            AccountStatus = AccountStatus.Approved
        };
        context.Users.AddRange(farmer, officer);

        var variety = new Variety
        {
            Name = $"Bg 352_{suffix}",
            DurationDays = 105,
            AgeGroup = "3.5 Months"
        };
        context.Varieties.Add(variety);

        var field = new Field
        {
            Name = $"Plot_{suffix}",
            Area = 2.0m,
            SoilType = "Clay",
            IrrigationType = "Canal",
            Division = division,
            Farmer = farmer,
            IsActive = true
        };
        context.Fields.Add(field);

        var cycle = new CultivationCycle
        {
            Field = field,
            Variety = variety,
            Season = Season.Maha,
            Year = 2026,
            Method = CultivationMethod.Broadcasting,
            SowingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-25)),
            ExpectedHarvestDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(80)),
            Status = CycleStatus.Active,
            CurrentStage = GrowthStage.Tillering
        };
        context.CultivationCycles.Add(cycle);

        var rec = new CropActivityRecommendation
        {
            RecommendationUid = $"rec_{suffix}",
            CultivationCycle = cycle,
            RequestedByUserId = farmer.Id,
            Category = "Fertilizer",
            Priority = "HIGH",
            Action = "Apply Top Dressing II: Urea 50 kg/ha",
            Reason = "Tillering stage nitrogen requirement",
            Evidence = "Last fertilizer logged 14 days ago",
            ConfidenceScore = 0.92,
            CitationsJson = "[]",
            Status = "PENDING_OFFICER_REVIEW",
            ExecutionPayloadJson = "{\"activityType\":\"Fertilizer\",\"detailsJson\":\"{\\\"type\\\":\\\"Urea\\\",\\\"quantity\\\":50,\\\"cropStage\\\":\\\"Tillering\\\",\\\"region\\\":\\\"Wet\\\",\\\"method\\\":\\\"Broadcasting\\\"}\"}",
            CreatedAt = DateTime.UtcNow
        };
        context.CropActivityRecommendations.Add(rec);

        await context.SaveChangesAsync();
        return (farmer, officer, cycle, rec);
    }

    [Fact]
    public async Task GetPendingOfficerRecommendations_AnonymousCaller_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/recommendations/pending");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task OfficerReviewRecommendation_FarmerCaller_Returns403Forbidden()
    {
        // Arrange: Farmers are forbidden from approving/rejecting officer queue recommendations
        var (farmer, _, _, rec) = await SeedRecommendationDataAsync();
        var client = _factory.CreateClient();
        var farmerToken = TestJwtHelper.GenerateToken(farmer.Id, farmer.Name, farmer.Email, "Farmer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", farmerToken);

        var payload = new OfficerRecommendationReviewDto
        {
            Decision = "Approve",
            Comment = "Attempted farmer approval"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/recommendations/{rec.Id}/officer-review", payload);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OfficerReviewRecommendation_AuthorizedOfficer_Returns200OkWithApprovedStatus()
    {
        // Arrange: AgriculturalOfficer can review and approve recommendation
        var (_, officer, _, rec) = await SeedRecommendationDataAsync();
        var client = _factory.CreateClient();
        var officerToken = TestJwtHelper.GenerateToken(officer.Id, officer.Name, officer.Email, "AgriculturalOfficer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", officerToken);

        var payload = new OfficerRecommendationReviewDto
        {
            Decision = "Approve",
            Comment = "Approved as per Sri Lanka DOA guidelines."
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/recommendations/{rec.Id}/officer-review", payload);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CropActivityRecommendationDto>();
        Assert.NotNull(result);
        Assert.Equal("APPROVED", result.Status);
        Assert.Equal("Approved as per Sri Lanka DOA guidelines.", result.OfficerComment);
    }

    [Fact]
    public async Task GetCycleRecommendations_AuthorizedUser_Returns200Ok()
    {
        // Arrange
        var (farmer, _, cycle, _) = await SeedRecommendationDataAsync();
        var client = _factory.CreateClient();
        var token = TestJwtHelper.GenerateToken(farmer.Id, farmer.Name, farmer.Email, "Farmer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/cycles/{cycle.Id}/recommendations");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
