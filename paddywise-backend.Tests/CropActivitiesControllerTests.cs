using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PaddyWise.Api.Data;
using PaddyWise.Api.DTOs.CropResource;
using PaddyWise.Api.Entities.CropResource;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.Shared;
using Xunit;

namespace PaddyWise.Backend.Tests;

public class CropActivitiesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CropActivitiesControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(User farmer, User otherFarmer, User officer, CultivationCycle cycle)> SeedDataAsync()
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
        var otherFarmer = new User
        {
            Name = $"OtherFarmer_{suffix}",
            Email = $"other_{suffix}@test.com",
            PasswordHash = "hash",
            Role = UserRole.Farmer,
            AccountStatus = AccountStatus.Approved
        };
        var officer = new User
        {
            Name = $"Officer_{suffix}",
            Email = $"officer_{suffix}@test.com",
            PasswordHash = "hash",
            Role = UserRole.AgriculturalOfficer,
            AccountStatus = AccountStatus.Approved
        };
        context.Users.AddRange(farmer, otherFarmer, officer);

        var variety = new Variety
        {
            Name = $"Bg 352_{suffix}",
            DurationDays = 105,
            AgeGroup = "3.5 Months"
        };
        context.Varieties.Add(variety);

        var field = new Field
        {
            Name = $"Field_{suffix}",
            Area = 3.0m,
            SoilType = "Clay Loam",
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
            SowingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-20)),
            ExpectedHarvestDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(85)),
            Status = CycleStatus.Active,
            CurrentStage = GrowthStage.Tillering
        };
        context.CultivationCycles.Add(cycle);

        await context.SaveChangesAsync();
        return (farmer, otherFarmer, officer, cycle);
    }

    // =========================================================================
    // 1. Authentication & Role Authorization Tests
    // =========================================================================

    [Fact]
    public async Task GetActivities_AnonymousCaller_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/cycles/1/activities");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAllActivities_AnonymousCaller_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/activities");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateActivity_OfficerCaller_Returns403Forbidden()
    {
        // Arrange: Only Farmers are allowed to log activities directly
        var (farmer, _, officer, cycle) = await SeedDataAsync();
        var client = _factory.CreateClient();
        var officerToken = TestJwtHelper.GenerateToken(officer.Id, officer.Name, officer.Email, "AgriculturalOfficer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", officerToken);

        var payload = new CreateCropActivityRequestDto
        {
            ActivityType = CropActivityType.Irrigation,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            DetailsJson = "{\"waterLevel\":5,\"duration\":2,\"source\":\"Canal\"}"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/cycles/{cycle.Id}/activities", payload);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // =========================================================================
    // 2. Functional Endpoints: Create, Get, Update, Delete
    // =========================================================================

    [Fact]
    public async Task CreateActivity_FarmerOwnerWithValidPayload_Returns201Created()
    {
        // Arrange
        var (farmer, _, _, cycle) = await SeedDataAsync();
        var client = _factory.CreateClient();
        var farmerToken = TestJwtHelper.GenerateToken(farmer.Id, farmer.Name, farmer.Email, "Farmer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", farmerToken);

        var payload = new CreateCropActivityRequestDto
        {
            ActivityType = CropActivityType.Irrigation,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            DetailsJson = "{\"waterLevel\":5,\"duration\":3,\"source\":\"Canal\"}"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/cycles/{cycle.Id}/activities", payload);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CropActivityDto>();
        Assert.NotNull(created);
        Assert.Equal("Irrigation", created.ActivityType);
    }

    [Fact]
    public async Task CreateActivity_FutureDate_Returns400BadRequest()
    {
        // Arrange
        var (farmer, _, _, cycle) = await SeedDataAsync();
        var client = _factory.CreateClient();
        var farmerToken = TestJwtHelper.GenerateToken(farmer.Id, farmer.Name, farmer.Email, "Farmer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", farmerToken);

        var payload = new CreateCropActivityRequestDto
        {
            ActivityType = CropActivityType.Irrigation,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), // future date
            DetailsJson = "{\"waterLevel\":5,\"duration\":3,\"source\":\"Canal\"}"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/cycles/{cycle.Id}/activities", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetActivities_FarmerOwner_Returns200OkWithList()
    {
        // Arrange
        var (farmer, _, _, cycle) = await SeedDataAsync();
        var client = _factory.CreateClient();
        var farmerToken = TestJwtHelper.GenerateToken(farmer.Id, farmer.Name, farmer.Email, "Farmer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", farmerToken);

        // Act
        var response = await client.GetAsync($"/api/cycles/{cycle.Id}/activities");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAllActivities_AuthorizedFarmer_Returns200Ok()
    {
        // Arrange
        var (farmer, _, _, _) = await SeedDataAsync();
        var client = _factory.CreateClient();
        var farmerToken = TestJwtHelper.GenerateToken(farmer.Id, farmer.Name, farmer.Email, "Farmer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", farmerToken);

        // Act
        var response = await client.GetAsync("/api/activities");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
