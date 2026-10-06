using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PaddyWise.Api.Agents.CropResource;
using PaddyWise.Api.Agents.Shared;
using PaddyWise.Api.Data;
using PaddyWise.Api.Entities.CropResource;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.Shared;
using PaddyWise.Backend.Tests.CropResource.Helpers;
using PaddyWise.Backend.Tests.FieldCultivation.Helpers;
using Xunit;

namespace PaddyWise.Backend.Tests.CropResource.Agent;

/// <summary>
/// The recommendation lifecycle the agent's output goes through:
/// PENDING_OFFICER_REVIEW → APPROVED / REJECTED (officer) → EXECUTED (farmer).
/// The LLM is not involved in any of these calls.
/// </summary>
[Trait("Component", "CropResource")]
public class RecommendationLifecycleTests
{
    private const int FarmerId = 1;
    private const int OfficerId = 50;

    private static ResourceAnalysisAgent CreateAgent(ApplicationDbContext db) =>
        new(db, Mock.Of<ILlmClient>(), NullLogger<ResourceAnalysisAgent>.Instance);

    private static async Task<(ApplicationDbContext Db, CultivationCycle Cycle)> SeedAsync()
    {
        var db = FcTestDb.CreateContext();
        var cycle = await FcTestDb.SeedFarmerCycleAsync(db, FarmerId, "QA Farmer");
        await FcTestDb.SeedUserAsync(db, OfficerId, "Officer Silva", UserRole.AgriculturalOfficer);
        return (db, cycle);
    }

    private static ReviewRecommendationRequestDto Farmer(CropActivityRecommendation rec, string decision) =>
        new() { RecommendationId = rec.RecommendationUid, Decision = decision };

    // ---------------------------------------------------------------- farmer

    [Fact]
    public async Task AG18a_ExecutingAnApprovedRecommendation_LogsTheActivityAndMarksItExecuted()
    {
        var (db, cycle) = await SeedAsync();
        using var _ = db;
        var rec = await CrSeed.AddRecommendationAsync(db, cycle, FarmerId, status: "APPROVED");

        var response = await CreateAgent(db).ReviewRecommendationAsync(cycle.Id, Farmer(rec, "Execute"), FarmerId, "Farmer");

        var activity = await db.CropActivities.SingleAsync();
        Assert.Equal(CropActivityType.Irrigation, activity.ActivityType);
        Assert.Equal(CrSeed.Today, activity.Date);
        Assert.Equal(activity.Id, response.CreatedActivityId);
        Assert.Equal("EXECUTED", response.Recommendation!.Status);

        db.ChangeTracker.Clear();
        var stored = await db.CropActivityRecommendations.SingleAsync();
        Assert.Equal("EXECUTED", stored.Status);
        Assert.Equal(activity.Id, stored.ExecutedActivityId);
        Assert.Equal(FarmerId, stored.ExecutedByUserId);
        Assert.Single(db.AgentRunLogs, l => l.AgentName == "ResourceAnalysisAgent:FarmerReview");
    }

    [Fact]
    public async Task AG18a2_AFarmerCannotActOnAnotherFarmersCycle()
    {
        var (db, cycle) = await SeedAsync();
        using var _ = db;
        var rec = await CrSeed.AddRecommendationAsync(db, cycle, FarmerId, status: "APPROVED");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => CreateAgent(db).ReviewRecommendationAsync(cycle.Id, Farmer(rec, "Execute"), 2, "Farmer"));

        Assert.Empty(db.CropActivities);
    }

    [Fact]
    public async Task AG18a3_RejectingARecommendation_StoresTheFarmersNote()
    {
        var (db, cycle) = await SeedAsync();
        using var _ = db;
        var rec = await CrSeed.AddRecommendationAsync(db, cycle, FarmerId, status: "APPROVED");
        var request = Farmer(rec, "reject");
        request.Notes = "Canal is closed this week.";

        var response = await CreateAgent(db).ReviewRecommendationAsync(cycle.Id, request, FarmerId, "Farmer");

        Assert.Equal("REJECTED", response.Recommendation!.Status);
        db.ChangeTracker.Clear();
        var stored = await db.CropActivityRecommendations.SingleAsync();
        Assert.Equal("REJECTED", stored.Status);
        Assert.Equal("Canal is closed this week.", stored.OfficerComment);
        Assert.Empty(db.CropActivities);
    }

    [Fact(Skip = "Known bug (finding 4): ReviewRecommendationAsync executes a recommendation whatever its status — a PENDING_OFFICER_REVIEW one becomes an activity without officer approval.")]
    public async Task AG18b_APendingRecommendation_CannotBeExecutedBeforeAnOfficerApprovesIt()
    {
        var (db, cycle) = await SeedAsync();
        using var _ = db;
        var rec = await CrSeed.AddRecommendationAsync(db, cycle, FarmerId, status: "PENDING_OFFICER_REVIEW");

        await Assert.ThrowsAnyAsync<Exception>(
            () => CreateAgent(db).ReviewRecommendationAsync(cycle.Id, Farmer(rec, "Execute"), FarmerId, "Farmer"));

        Assert.Empty(db.CropActivities);
        db.ChangeTracker.Clear();
        Assert.Equal("PENDING_OFFICER_REVIEW", (await db.CropActivityRecommendations.SingleAsync()).Status);
    }

    [Fact(Skip = "Known bug (finding 5): any farmer decision other than execute/reject is answered with Status = \"APPROVED\", although no officer approved anything.")]
    public async Task AG18c_AFarmerDecisionOfApprove_IsNeverReportedAsApproved()
    {
        var (db, cycle) = await SeedAsync();
        using var _ = db;
        var rec = await CrSeed.AddRecommendationAsync(db, cycle, FarmerId, status: "PENDING_OFFICER_REVIEW");

        var response = await CreateAgent(db).ReviewRecommendationAsync(cycle.Id, Farmer(rec, "Approve"), FarmerId, "Farmer");

        Assert.NotEqual("APPROVED", response.Recommendation!.Status);
    }

    // ---------------------------------------------------------------- officer

    [Theory]
    [InlineData("Approve", "APPROVED")]
    [InlineData("approve", "APPROVED")]
    [InlineData("Reject", "REJECTED")]
    public async Task AG19a_OfficerReview_SetsTheStatusAndTellsTheFarmer(string decision, string expected)
    {
        var (db, cycle) = await SeedAsync();
        using var _ = db;
        var rec = await CrSeed.AddRecommendationAsync(db, cycle, FarmerId);

        var dto = await CreateAgent(db).OfficerReviewRecommendationAsync(rec.Id, decision, "Checked against DOA tables.", OfficerId);

        Assert.Equal(expected, dto.Status);
        Assert.Equal("Officer Silva", dto.OfficerName);
        Assert.Equal("Checked against DOA tables.", dto.OfficerComment);
        Assert.NotNull(dto.ReviewedAt);

        var notification = await db.Notifications.SingleAsync();
        Assert.Equal(FarmerId, notification.UserId);
        Assert.Equal(expected, notification.Status);
        Assert.Equal(rec.Id, notification.RelatedRecommendationId);
        Assert.Single(db.AgentRunLogs);
    }

    [Fact]
    public async Task AG19a2_OfficerReview_OfAnUnknownRecommendation_IsRefused()
    {
        var (db, _) = await SeedAsync();
        using var __ = db;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateAgent(db).OfficerReviewRecommendationAsync(9999, "Approve", null, OfficerId));

        Assert.Equal("Recommendation #9999 not found.", ex.Message);
    }

    [Fact(Skip = "Known bug (finding 6): OfficerReviewRecommendationAsync treats every decision except exactly \"Approve\" as a rejection — the typo \"Approved\" rejects the recommendation.")]
    public async Task AG19b_AnUnrecognisedOfficerDecision_IsRefused_NotTreatedAsReject()
    {
        var (db, cycle) = await SeedAsync();
        using var _ = db;
        var rec = await CrSeed.AddRecommendationAsync(db, cycle, FarmerId);

        await Assert.ThrowsAnyAsync<Exception>(
            () => CreateAgent(db).OfficerReviewRecommendationAsync(rec.Id, "Approved", null, OfficerId));

        db.ChangeTracker.Clear();
        Assert.Equal("PENDING_OFFICER_REVIEW", (await db.CropActivityRecommendations.SingleAsync()).Status);
    }

    [Fact(Skip = "Known bug (finding 6): OfficerReviewRecommendationAsync has no status check — an EXECUTED recommendation can be re-reviewed and flipped to REJECTED after the farmer acted on it.")]
    public async Task AG19c_AnExecutedRecommendation_CannotBeReReviewed()
    {
        var (db, cycle) = await SeedAsync();
        using var _ = db;
        var rec = await CrSeed.AddRecommendationAsync(db, cycle, FarmerId, status: "EXECUTED");

        await Assert.ThrowsAnyAsync<Exception>(
            () => CreateAgent(db).OfficerReviewRecommendationAsync(rec.Id, "Reject", "Changed my mind.", OfficerId));

        db.ChangeTracker.Clear();
        Assert.Equal("EXECUTED", (await db.CropActivityRecommendations.SingleAsync()).Status);
    }

    [Fact]
    public async Task AG19d_TheExecutionPayload_DecidesTheLoggedActivity()
    {
        var (db, cycle) = await SeedAsync();
        using var _ = db;
        var payload = JsonSerializer.Serialize(new { activityType = "Fertilizer", detailsJson = CrSeed.Fertilizer("Urea", 50) });
        var rec = await CrSeed.AddRecommendationAsync(db, cycle, FarmerId, status: "APPROVED", executionPayloadJson: payload);

        await CreateAgent(db).ReviewRecommendationAsync(cycle.Id, Farmer(rec, "approve_and_execute"), FarmerId, "Farmer");

        var activity = await db.CropActivities.SingleAsync();
        Assert.Equal(CropActivityType.Fertilizer, activity.ActivityType);
        Assert.Contains("\"type\":\"Urea\"", activity.DetailsJson);
    }
}
