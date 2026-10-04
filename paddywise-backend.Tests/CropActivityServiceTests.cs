using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PaddyWise.Api.Data;
using PaddyWise.Api.DTOs.CropResource;
using PaddyWise.Api.Entities.CropResource;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.Shared;
using PaddyWise.Api.Services.CropResource;
using Xunit;

namespace PaddyWise.Backend.Tests;

public class CropActivityServiceTests
{
    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "CropActivityServiceDb_" + Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<(User farmer, User otherFarmer, User officer, CultivationCycle cycle)> SeedBaseDataAsync(ApplicationDbContext context)
    {
        var division = new Division
        {
            Name = "Polonnaruwa Central",
            District = "Polonnaruwa",
            Province = "North Central"
        };
        context.Divisions.Add(division);

        var farmer = new User
        {
            Name = "Sunil Bandara",
            Email = "sunil@farmer.com",
            PasswordHash = "hash",
            Role = UserRole.Farmer,
            AccountStatus = AccountStatus.Approved
        };
        var otherFarmer = new User
        {
            Name = "Kamal Perera",
            Email = "kamal@farmer.com",
            PasswordHash = "hash",
            Role = UserRole.Farmer,
            AccountStatus = AccountStatus.Approved
        };
        var officer = new User
        {
            Name = "Dr. Nilmini Perera",
            Email = "nilmini@gov.lk",
            PasswordHash = "hash",
            Role = UserRole.AgriculturalOfficer,
            AccountStatus = AccountStatus.Approved
        };
        context.Users.AddRange(farmer, otherFarmer, officer);

        var variety = new Variety
        {
            Name = "Bg 352",
            DurationDays = 105,
            AgeGroup = "3.5 Months"
        };
        context.Varieties.Add(variety);

        var field = new Field
        {
            Name = "Maha Kumbura",
            Area = 2.5m,
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
            SowingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            ExpectedHarvestDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(75)),
            Status = CycleStatus.Active,
            CurrentStage = GrowthStage.Tillering
        };
        context.CultivationCycles.Add(cycle);

        await context.SaveChangesAsync();
        return (farmer, otherFarmer, officer, cycle);
    }

    // =========================================================================
    // 1. GetActivitiesForCycleAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetActivitiesForCycleAsync_FarmerOwner_ReturnsActivitiesOrderedByDateDesc()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (farmer, _, _, cycle) = await SeedBaseDataAsync(context);

        var act1 = new CropActivity
        {
            CultivationCycleId = cycle.Id,
            ActivityType = CropActivityType.Irrigation,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
            DetailsJson = "{\"waterLevel\":5,\"duration\":3,\"source\":\"Canal\"}",
            LoggedByUserId = farmer.Id,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-5)
        };
        var act2 = new CropActivity
        {
            CultivationCycleId = cycle.Id,
            ActivityType = CropActivityType.Fertilizer,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
            DetailsJson = "{\"type\":\"Urea\",\"quantity\":50,\"cropStage\":\"Tillering\",\"region\":\"Wet\",\"method\":\"Broadcasting\"}",
            LoggedByUserId = farmer.Id,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };
        context.CropActivities.AddRange(act1, act2);
        await context.SaveChangesAsync();

        var service = new CropActivityService(context);

        // Act
        var result = await service.GetActivitiesForCycleAsync(cycle.Id, farmer.Id, "Farmer");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Fertilizer", result[0].ActivityType); // More recent date first
        Assert.Equal("Irrigation", result[1].ActivityType);
        Assert.Equal("Maha Kumbura", result[0].FieldName);
        Assert.Equal("Sunil Bandara", result[0].FarmerName);
    }

    [Fact]
    public async Task GetActivitiesForCycleAsync_UnauthorizedFarmer_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (_, otherFarmer, _, cycle) = await SeedBaseDataAsync(context);
        var service = new CropActivityService(context);

        // Act & Assert: otherFarmer does not own cycle's field
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetActivitiesForCycleAsync(cycle.Id, otherFarmer.Id, "Farmer"));
    }

    [Fact]
    public async Task GetActivitiesForCycleAsync_AgriculturalOfficer_CanAccessAnyCycle()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (_, _, officer, cycle) = await SeedBaseDataAsync(context);
        var service = new CropActivityService(context);

        // Act
        var result = await service.GetActivitiesForCycleAsync(cycle.Id, officer.Id, "AgriculturalOfficer");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetActivitiesForCycleAsync_NonExistentCycle_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (farmer, _, _, _) = await SeedBaseDataAsync(context);
        var service = new CropActivityService(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetActivitiesForCycleAsync(99999, farmer.Id, "Farmer"));
    }

    // =========================================================================
    // 2. CreateActivityAsync - Success & Details Validation Tests
    // =========================================================================

    [Fact]
    public async Task CreateActivityAsync_ValidFertilizerActivity_CreatesAndReturnsDto()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (farmer, _, _, cycle) = await SeedBaseDataAsync(context);
        var service = new CropActivityService(context);

        var request = new CreateCropActivityRequestDto
        {
            ActivityType = CropActivityType.Fertilizer,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            DetailsJson = "{\"type\":\"Urea\",\"quantity\":50,\"cropStage\":\"Tillering\",\"region\":\"Wet\",\"method\":\"Broadcasting\"}"
        };

        // Act
        var created = await service.CreateActivityAsync(cycle.Id, request, farmer.Id);

        // Assert
        Assert.NotNull(created);
        Assert.Equal("Fertilizer", created.ActivityType);
        Assert.Equal(farmer.Id, created.LoggedByUserId);
        Assert.Equal(farmer.Name, created.LoggedByUserName);

        var dbActivity = await context.CropActivities.FirstOrDefaultAsync(a => a.Id == created.Id);
        Assert.NotNull(dbActivity);
        Assert.Equal(CropActivityType.Fertilizer, dbActivity.ActivityType);
    }

    [Fact]
    public async Task CreateActivityAsync_ValidIrrigationActivity_CreatesSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (farmer, _, _, cycle) = await SeedBaseDataAsync(context);
        var service = new CropActivityService(context);

        var request = new CreateCropActivityRequestDto
        {
            ActivityType = CropActivityType.Irrigation,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            DetailsJson = "{\"waterLevel\":5,\"duration\":2.5,\"source\":\"Canal\"}"
        };

        // Act
        var created = await service.CreateActivityAsync(cycle.Id, request, farmer.Id);

        // Assert
        Assert.NotNull(created);
        Assert.Equal("Irrigation", created.ActivityType);
    }

    [Fact]
    public async Task CreateActivityAsync_ValidPesticideActivity_CreatesSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (farmer, _, _, cycle) = await SeedBaseDataAsync(context);
        var service = new CropActivityService(context);

        var request = new CreateCropActivityRequestDto
        {
            ActivityType = CropActivityType.Pesticide,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            DetailsJson = "{\"product\":\"Chlorantraniliprole\",\"targetPest\":\"Stem Borer\",\"quantity\":100,\"method\":\"Spraying\"}"
        };

        // Act
        var created = await service.CreateActivityAsync(cycle.Id, request, farmer.Id);

        // Assert
        Assert.NotNull(created);
        Assert.Equal("Pesticide", created.ActivityType);
    }

    [Fact]
    public async Task CreateActivityAsync_ValidOtherActivity_CreatesSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (farmer, _, _, cycle) = await SeedBaseDataAsync(context);
        var service = new CropActivityService(context);

        var request = new CreateCropActivityRequestDto
        {
            ActivityType = CropActivityType.Other,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            DetailsJson = "{\"specificActivity\":\"Weeding\",\"notes\":\"Manual weeding completed\"}"
        };

        // Act
        var created = await service.CreateActivityAsync(cycle.Id, request, farmer.Id);

        // Assert
        Assert.NotNull(created);
        Assert.Equal("Other", created.ActivityType);
    }

    // =========================================================================
    // 3. CreateActivityAsync - Date & Ownership Validation Boundary Tests
    // =========================================================================

    [Fact]
    public async Task CreateActivityAsync_DateInFuture_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (farmer, _, _, cycle) = await SeedBaseDataAsync(context);
        var service = new CropActivityService(context);

        var request = new CreateCropActivityRequestDto
        {
            ActivityType = CropActivityType.Irrigation,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)), // Future date
            DetailsJson = "{\"waterLevel\":5,\"duration\":2,\"source\":\"Canal\"}"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateActivityAsync(cycle.Id, request, farmer.Id));
        Assert.Contains("future", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateActivityAsync_DateOlderThanOneWeek_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (farmer, _, _, cycle) = await SeedBaseDataAsync(context);
        var service = new CropActivityService(context);

        var request = new CreateCropActivityRequestDto
        {
            ActivityType = CropActivityType.Irrigation,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)), // 10 days ago (older than 7 days)
            DetailsJson = "{\"waterLevel\":5,\"duration\":2,\"source\":\"Canal\"}"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateActivityAsync(cycle.Id, request, farmer.Id));
        Assert.Contains("last 7 days", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateActivityAsync_DateEarlierThanCycleSowingDate_ThrowsArgumentException()
    {
        // Arrange: Sowing date was 30 days ago, but if cycle was sown yesterday and activity is 3 days ago
        using var context = CreateInMemoryDbContext();
        var (farmer, _, _, cycle) = await SeedBaseDataAsync(context);
        cycle.SowingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));
        await context.SaveChangesAsync();

        var service = new CropActivityService(context);
        var request = new CreateCropActivityRequestDto
        {
            ActivityType = CropActivityType.Irrigation,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-4)), // before sowing date
            DetailsJson = "{\"waterLevel\":5,\"duration\":2,\"source\":\"Canal\"}"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateActivityAsync(cycle.Id, request, farmer.Id));
        Assert.Contains("sowing date", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateActivityAsync_DateAfterActualHarvestDate_ThrowsArgumentException()
    {
        // Arrange: Cycle already harvested 2 days ago
        using var context = CreateInMemoryDbContext();
        var (farmer, _, _, cycle) = await SeedBaseDataAsync(context);
        cycle.ActualHarvestDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));
        await context.SaveChangesAsync();

        var service = new CropActivityService(context);
        var request = new CreateCropActivityRequestDto
        {
            ActivityType = CropActivityType.Irrigation,
            Date = DateOnly.FromDateTime(DateTime.UtcNow), // after harvest date
            DetailsJson = "{\"waterLevel\":5,\"duration\":2,\"source\":\"Canal\"}"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateActivityAsync(cycle.Id, request, farmer.Id));
        Assert.Contains("harvest date", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateActivityAsync_NonOwnerFarmer_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (_, otherFarmer, _, cycle) = await SeedBaseDataAsync(context);
        var service = new CropActivityService(context);

        var request = new CreateCropActivityRequestDto
        {
            ActivityType = CropActivityType.Irrigation,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            DetailsJson = "{\"waterLevel\":5,\"duration\":2,\"source\":\"Canal\"}"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CreateActivityAsync(cycle.Id, request, otherFarmer.Id));
    }

    // =========================================================================
    // 4. DetailsJson Validation Tests for Different Types
    // =========================================================================

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-json")]
    public async Task CreateActivityAsync_InvalidDetailsJson_ThrowsArgumentException(string invalidJson)
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (farmer, _, _, cycle) = await SeedBaseDataAsync(context);
        var service = new CropActivityService(context);

        var request = new CreateCropActivityRequestDto
        {
            ActivityType = CropActivityType.Irrigation,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            DetailsJson = invalidJson
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateActivityAsync(cycle.Id, request, farmer.Id));
    }

    [Fact]
    public async Task CreateActivityAsync_FertilizerWithZeroOrNegativeQuantity_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (farmer, _, _, cycle) = await SeedBaseDataAsync(context);
        var service = new CropActivityService(context);

        var request = new CreateCropActivityRequestDto
        {
            ActivityType = CropActivityType.Fertilizer,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            DetailsJson = "{\"type\":\"Urea\",\"quantity\":-10,\"cropStage\":\"Tillering\",\"region\":\"Wet\",\"method\":\"Broadcasting\"}"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateActivityAsync(cycle.Id, request, farmer.Id));
        Assert.Contains("positive number", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateActivityAsync_IrrigationWithNegativeWaterLevel_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (farmer, _, _, cycle) = await SeedBaseDataAsync(context);
        var service = new CropActivityService(context);

        var request = new CreateCropActivityRequestDto
        {
            ActivityType = CropActivityType.Irrigation,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            DetailsJson = "{\"waterLevel\":-2,\"duration\":3,\"source\":\"Canal\"}"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateActivityAsync(cycle.Id, request, farmer.Id));
        Assert.Contains("non-negative", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================================
    // 5. UpdateActivityAsync and DeleteActivityAsync Tests
    // =========================================================================

    [Fact]
    public async Task UpdateActivityAsync_FarmerOwner_UpdatesSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (farmer, _, _, cycle) = await SeedBaseDataAsync(context);

        var activity = new CropActivity
        {
            CultivationCycleId = cycle.Id,
            ActivityType = CropActivityType.Fertilizer,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
            DetailsJson = "{\"type\":\"Urea\",\"quantity\":50,\"cropStage\":\"Tillering\",\"region\":\"Wet\",\"method\":\"Broadcasting\"}",
            LoggedByUserId = farmer.Id,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };
        context.CropActivities.Add(activity);
        await context.SaveChangesAsync();

        var service = new CropActivityService(context);

        var updateRequest = new UpdateCropActivityRequestDto
        {
            ActivityType = CropActivityType.Fertilizer,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            DetailsJson = "{\"type\":\"Urea\",\"quantity\":60,\"cropStage\":\"Tillering\",\"region\":\"Wet\",\"method\":\"Broadcasting\"}"
        };

        // Act
        var updated = await service.UpdateActivityAsync(activity.Id, updateRequest, farmer.Id, "Farmer");

        // Assert
        Assert.Equal(activity.Id, updated.Id);
        Assert.Contains("\"quantity\":60", updated.DetailsJson);
    }

    [Fact]
    public async Task UpdateActivityAsync_DifferentFarmer_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (farmer, otherFarmer, _, cycle) = await SeedBaseDataAsync(context);

        var activity = new CropActivity
        {
            CultivationCycleId = cycle.Id,
            ActivityType = CropActivityType.Fertilizer,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
            DetailsJson = "{\"type\":\"Urea\",\"quantity\":50,\"cropStage\":\"Tillering\",\"region\":\"Wet\",\"method\":\"Broadcasting\"}",
            LoggedByUserId = farmer.Id,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };
        context.CropActivities.Add(activity);
        await context.SaveChangesAsync();

        var service = new CropActivityService(context);

        var updateRequest = new UpdateCropActivityRequestDto
        {
            ActivityType = CropActivityType.Fertilizer,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            DetailsJson = "{\"type\":\"Urea\",\"quantity\":60,\"cropStage\":\"Tillering\",\"region\":\"Wet\",\"method\":\"Broadcasting\"}"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UpdateActivityAsync(activity.Id, updateRequest, otherFarmer.Id, "Farmer"));
    }

    [Fact]
    public async Task DeleteActivityAsync_FarmerOwner_DeletesSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (farmer, _, _, cycle) = await SeedBaseDataAsync(context);

        var activity = new CropActivity
        {
            CultivationCycleId = cycle.Id,
            ActivityType = CropActivityType.Irrigation,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            DetailsJson = "{\"waterLevel\":5,\"duration\":2,\"source\":\"Canal\"}",
            LoggedByUserId = farmer.Id,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        context.CropActivities.Add(activity);
        await context.SaveChangesAsync();

        var service = new CropActivityService(context);

        // Act
        await service.DeleteActivityAsync(activity.Id, farmer.Id, "Farmer");

        // Assert
        var deleted = await context.CropActivities.FindAsync(activity.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteActivityAsync_NonExistentActivity_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (farmer, _, _, _) = await SeedBaseDataAsync(context);
        var service = new CropActivityService(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteActivityAsync(99999, farmer.Id, "Farmer"));
    }
}
