using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PaddyWise.Api.Agents.Shared;
using PaddyWise.Api.Data;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.PestDisease;
using PaddyWise.Api.Services.ReportingApproval;
using PaddyWise.Backend.Tests.FieldCultivation.Helpers;
using PaddyWise.Backend.Tests.ReportingApproval.Helpers;
using Xunit;

namespace PaddyWise.Backend.Tests.ReportingApproval.Agent;

/// <summary>
/// NotificationMessageService — gaps next to Virul's NotificationMessageServiceTests: the
/// remaining prompt branches, an empty reply, unknown ids, who receives the notification,
/// and whether LLM text is checked before it reaches the farmer.
/// </summary>
[Trait("Component", "ReportingApproval")]
public class NotificationMessageGapTests
{
    private const int FarmerId = 1;

    private static NotificationMessageService Create(ApplicationDbContext db, Mock<ILlmClient> llm) =>
        new(db, llm.Object, NullLogger<NotificationMessageService>.Instance);

    private static Mock<ILlmClient> Llm(List<FakePlanningLlmClient.CapturedCall> captured, string reply = "Your plan was reviewed.") =>
        FakePlanningLlmClient.Create((_, _, _, _) => Task.FromResult(reply), captured);

    [Theory]
    [InlineData(PlanStatus.RevisionRequested, "Add a water step at flowering.", "needs changes, based on this officer feedback: 'Add a water step at flowering.'")]
    [InlineData(PlanStatus.PendingOfficerApproval, null, "cultivation plan status is now 'PendingOfficerApproval'")]
    [InlineData(PlanStatus.Draft, null, "cultivation plan status is now 'Draft'")]
    public async Task RAAG15a_PlanPromptBranches(PlanStatus status, string? comment, string expectedPrompt)
    {
        using var db = FcTestDb.CreateContext();
        var cycle = await FcTestDb.SeedFarmerCycleAsync(db, FarmerId, "QA Farmer");
        var plan = await RaSeed.PlanAsync(db, cycle, FarmerId, "{}", status: status, officerComment: comment);
        var captured = new List<FakePlanningLlmClient.CapturedCall>();

        var notification = await Create(db, Llm(captured)).GenerateAndCreateCultivationPlanNotificationAsync(plan.Id);

        Assert.Contains(expectedPrompt, Assert.Single(captured).UserPrompt);
        Assert.Equal(status.ToString(), notification!.Status);
        Assert.Equal(comment, notification.OfficerComment);
    }

    [Fact]
    public async Task RAAG15b_ReportRevisionRequested_UsesTheOfficerComment()
    {
        using var db = FcTestDb.CreateContext();
        var cycle = await FcTestDb.SeedFarmerCycleAsync(db, FarmerId, "QA Farmer");
        var report = await RaSeed.ReportAsync(db, cycle, FarmerId,
            status: PestDiseaseReportStatus.RevisionRequested, officerComment: "Send a closer photo.");
        var captured = new List<FakePlanningLlmClient.CapturedCall>();

        await Create(db, Llm(captured)).GenerateAndCreatePestDiseaseReportNotificationAsync(report.Id);

        Assert.Contains("needs changes, based on this officer feedback: 'Send a closer photo.'", Assert.Single(captured).UserPrompt);
    }

    [Fact]
    public async Task RAAG16a_AnEmptyReply_UsesTheFallbackMessage_AndLogsTheFailure()
    {
        using var db = FcTestDb.CreateContext();
        var cycle = await FcTestDb.SeedFarmerCycleAsync(db, FarmerId, "QA Farmer");
        var plan = await RaSeed.PlanAsync(db, cycle, FarmerId, "{}", status: PlanStatus.Approved);

        var notification = await Create(db, Llm(new(), reply: " ")).GenerateAndCreateCultivationPlanNotificationAsync(plan.Id);

        Assert.Equal("Your cultivation plan status has been updated to Approved. Check the app for details.", notification!.Message);
        var log = await db.AgentRunLogs.SingleAsync();
        Assert.False(log.Success);
        Assert.Equal("LLM returned an empty response.", log.Error);
    }

    [Fact]
    public async Task RAAG16b_UnknownPlanOrReport_ReturnNull_AndCreateNoNotification()
    {
        using var db = FcTestDb.CreateContext();
        var service = Create(db, Llm(new()));

        Assert.Null(await service.GenerateAndCreateCultivationPlanNotificationAsync(9999));
        Assert.Null(await service.GenerateAndCreatePestDiseaseReportNotificationAsync(9999));
        Assert.Empty(db.Notifications);
    }

    [Fact]
    public async Task RAAG16c_TheNotificationGoesToTheFarmerWhoAskedOrReported()
    {
        using var db = FcTestDb.CreateContext();
        var cycle = await FcTestDb.SeedFarmerCycleAsync(db, FarmerId, "QA Farmer");
        var plan = await RaSeed.PlanAsync(db, cycle, FarmerId, "{}", status: PlanStatus.Approved);
        var report = await RaSeed.ReportAsync(db, cycle, reportedByUserId: 7, status: PestDiseaseReportStatus.Approved);
        var service = Create(db, Llm(new()));

        var planNote = await service.GenerateAndCreateCultivationPlanNotificationAsync(plan.Id);
        var reportNote = await service.GenerateAndCreatePestDiseaseReportNotificationAsync(report.Id);

        Assert.Equal(FarmerId, planNote!.UserId);
        Assert.Equal("CultivationPlanReview", planNote.Type);
        Assert.Equal(NotificationMessageService.PlanNotificationTitle, planNote.Title);
        Assert.Equal(7, reportNote!.UserId);
        Assert.Equal("PestDiseaseReportReview", reportNote.Type);
        Assert.False(reportNote.IsRead);
    }

    [Fact(Skip = "Known bug (finding 5): the LLM's notification text is stored and shown to the farmer verbatim, with no deterministic check (CLAUDE.md §8) — a dosage the officer never approved reaches the farmer.")]
    public async Task RAAG17_ANotificationWithADosage_IsNotDeliveredVerbatim()
    {
        using var db = FcTestDb.CreateContext();
        var cycle = await FcTestDb.SeedFarmerCycleAsync(db, FarmerId, "QA Farmer");
        var plan = await RaSeed.PlanAsync(db, cycle, FarmerId, "{}", status: PlanStatus.Approved);

        var notification = await Create(db, Llm(new(), reply: "Approved! Apply 50 kg urea before Friday."))
            .GenerateAndCreateCultivationPlanNotificationAsync(plan.Id);

        Assert.DoesNotContain("50 kg", notification!.Message);
    }
}
