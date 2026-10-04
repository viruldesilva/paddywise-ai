using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PaddyWise.Api.Agents.CropResource;
using PaddyWise.Api.Agents.Shared;
using PaddyWise.Api.Data;
using PaddyWise.Api.Entities.CropResource;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.Shared;
using Xunit;

namespace PaddyWise.Backend.Tests;

public class CropActivityRecommendationWorkflowTests
{
    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "CropActivityWorkflowDb_" + Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<(User farmer, User officer, CultivationCycle cycle, CropActivityRecommendation rec)> SeedBaseWorkflowDataAsync(ApplicationDbContext context)
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
        var officer = new User
        {
            Name = "Dr. Nilmini Perera",
            Email = "nilmini@gov.lk",
            PasswordHash = "hash",
            Role = UserRole.AgriculturalOfficer,
            AccountStatus = AccountStatus.Approved
        };
        context.Users.AddRange(farmer, officer);

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

        var recommendation = new CropActivityRecommendation
        {
            RecommendationUid = "rec_001",
            CultivationCycle = cycle,
            RequestedByUserId = farmer.Id,
            Category = "Fertilizer",
            Priority = "HIGH",
            Action = "Apply Top Dressing II: Urea 50 kg/ha",
            Reason = "Tillering stage nitrogen requirement",
            Evidence = "Last fertilizer logged 14 days ago",
            ConfidenceScore = 0.94,
            CitationsJson = "[]",
            Status = "PENDING_OFFICER_REVIEW",
            ExecutionPayloadJson = "{\"activityType\":\"Fertilizer\",\"detailsJson\":\"{\\\"type\\\":\\\"Urea\\\",\\\"quantity\\\":50,\\\"cropStage\\\":\\\"Tillering\\\",\\\"region\\\":\\\"Wet\\\",\\\"method\\\":\\\"Broadcasting\\\"}\"}",
            CreatedAt = DateTime.UtcNow
        };
        context.CropActivityRecommendations.Add(recommendation);

        await context.SaveChangesAsync();
        return (farmer, officer, cycle, recommendation);
    }

    [Fact]
    public async Task OfficerReview_ApproveRecommendation_UpdatesStatusAndDispatchesNotification()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (farmer, officer, _, rec) = await SeedBaseWorkflowDataAsync(context);

        var mockLlm = new Mock<ILlmClient>();
        var agent = new ResourceAnalysisAgent(context, mockLlm.Object, NullLogger<ResourceAnalysisAgent>.Instance);

        // Act: Officer approves recommendation
        var result = await agent.OfficerReviewRecommendationAsync(
            rec.Id,
            "Approve",
            "Approved as per Sri Lanka DOA 2023 paddy fertilizer guidelines.",
            officer.Id
        );

        // Assert: Recommendation updated
        Assert.Equal("APPROVED", result.Status);
        Assert.Equal(officer.Name, result.OfficerName);
        Assert.Equal("Approved as per Sri Lanka DOA 2023 paddy fertilizer guidelines.", result.OfficerComment);

        var dbRec = await context.CropActivityRecommendations.FindAsync(rec.Id);
        Assert.NotNull(dbRec);
        Assert.Equal("APPROVED", dbRec.Status);

        // Assert: Notification created for farmer
        var notification = await context.Notifications
            .FirstOrDefaultAsync(n => n.UserId == farmer.Id && n.RelatedRecommendationId == rec.Id);

        Assert.NotNull(notification);
        Assert.Equal("CropActivityReview", notification.Type);
        Assert.Equal("APPROVED", notification.Status);
        Assert.Contains(officer.Name, notification.Message);
    }

    [Fact]
    public async Task OfficerReview_RejectRecommendation_UpdatesStatusToRejected()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (farmer, officer, _, rec) = await SeedBaseWorkflowDataAsync(context);

        var mockLlm = new Mock<ILlmClient>();
        var agent = new ResourceAnalysisAgent(context, mockLlm.Object, NullLogger<ResourceAnalysisAgent>.Instance);

        // Act: Officer rejects recommendation
        var result = await agent.OfficerReviewRecommendationAsync(
            rec.Id,
            "Reject",
            "Heavy rain forecast for the next 48 hours; postpone broadcasting.",
            officer.Id
        );

        // Assert: Recommendation updated
        Assert.Equal("REJECTED", result.Status);
        Assert.Equal(officer.Name, result.OfficerName);

        var dbRec = await context.CropActivityRecommendations.FindAsync(rec.Id);
        Assert.NotNull(dbRec);
        Assert.Equal("REJECTED", dbRec.Status);

        // Assert: Notification sent to farmer
        var notification = await context.Notifications
            .FirstOrDefaultAsync(n => n.UserId == farmer.Id && n.RelatedRecommendationId == rec.Id);

        Assert.NotNull(notification);
        Assert.Equal("REJECTED", notification.Status);
        Assert.Contains("postpone", notification.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FarmerReview_ExecuteRecommendation_CreatesCropActivityAndMarksExecuted()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (farmer, officer, cycle, rec) = await SeedBaseWorkflowDataAsync(context);

        // First, mark as approved
        rec.Status = "APPROVED";
        rec.OfficerId = officer.Id;
        rec.OfficerName = officer.Name;
        rec.ReviewedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        var mockLlm = new Mock<ILlmClient>();
        var agent = new ResourceAnalysisAgent(context, mockLlm.Object, NullLogger<ResourceAnalysisAgent>.Instance);

        var request = new ReviewRecommendationRequestDto
        {
            RecommendationId = rec.Id.ToString(),
            Decision = "Execute",
            Notes = "Executed in the morning."
        };

        // Act: Farmer executes recommendation
        var response = await agent.ReviewRecommendationAsync(
            cycle.Id,
            request,
            farmer.Id,
            "Farmer"
        );

        // Assert: Response indicates success and activity creation
        Assert.True(response.Success);
        Assert.NotNull(response.CreatedActivityId);
        Assert.NotNull(response.Recommendation);
        Assert.Equal("EXECUTED", response.Recommendation.Status);

        // Assert: Concrete CropActivity exists in database
        var createdActivity = await context.CropActivities.FindAsync(response.CreatedActivityId);
        Assert.NotNull(createdActivity);
        Assert.Equal(CropActivityType.Fertilizer, createdActivity.ActivityType);
        Assert.Equal(farmer.Id, createdActivity.LoggedByUserId);
        Assert.Contains("Urea", createdActivity.DetailsJson);

        // Assert: Recommendation marked as EXECUTED in database
        var dbRec = await context.CropActivityRecommendations.FindAsync(rec.Id);
        Assert.NotNull(dbRec);
        Assert.Equal("EXECUTED", dbRec.Status);
        Assert.Equal(createdActivity.Id, dbRec.ExecutedActivityId);
        Assert.Equal(farmer.Id, dbRec.ExecutedByUserId);
        Assert.NotNull(dbRec.ExecutedAt);
    }
}
