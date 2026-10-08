using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PaddyWise.Api.Agents.Shared;
using PaddyWise.Api.Data;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.PestDisease;
using PaddyWise.Api.Services.ReportingApproval;
using Xunit;

namespace PaddyWise.Backend.Tests;

[Trait("Component", "ReportingApproval")]
public class NotificationMessageServiceTests
{
    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "NotificationTestDb_" + Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    // 1. Normal Case: Approved decision -> Notification created with LLM-generated message
    [Fact]
    public async Task CultivationPlan_ApprovedDecision_CreatesNotificationWithGeneratedMessage()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockLlm = new Mock<ILlmClient>();

        var expectedLlmMessage = "Great news! Your cultivation plan has been officially approved. You may proceed with land preparation.";
        mockLlm
            .Setup(l => l.CompleteJsonAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<LlmToolDefinition>>(),
                It.IsAny<Func<string, string, Task<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedLlmMessage);

        var plan = new CultivationPlan
        {
            Id = 101,
            CultivationCycleId = 1,
            RequestedByUserId = 42,
            Objective = "Maximize yield for Maha 2026",
            Status = PlanStatus.Approved,
            OfficerComment = "Comprehensive and realistic schedule. Well balanced."
        };
        context.CultivationPlans.Add(plan);
        await context.SaveChangesAsync();

        var service = new NotificationMessageService(
            context,
            mockLlm.Object,
            NullLogger<NotificationMessageService>.Instance);

        // Act
        var notification = await service.GenerateAndCreateCultivationPlanNotificationAsync(plan.Id);

        // Assert
        Assert.NotNull(notification);
        Assert.Equal(42, notification.UserId);
        Assert.Equal("Cultivation Plan Update", notification.Title);
        Assert.Equal(expectedLlmMessage, notification.Message);
        Assert.False(notification.IsRead);

        // Verify database persistence
        var dbNotification = await context.Notifications.FirstOrDefaultAsync(n => n.UserId == 42);
        Assert.NotNull(dbNotification);
        Assert.Equal(expectedLlmMessage, dbNotification.Message);

        // Verify AgentRunLog audit trail
        var log = await context.AgentRunLogs.FirstOrDefaultAsync(l => l.CultivationPlanId == plan.Id);
        Assert.NotNull(log);
        Assert.Equal("NotificationMessageAgent", log.AgentName);
        Assert.True(log.Success);
        Assert.Equal(expectedLlmMessage, log.RawOutput);
    }

    // 2. Normal Case: Rejected decision with OfficerComment -> prompt references comment substance
    [Fact]
    public async Task CultivationPlan_RejectedDecisionWithOfficerComment_MessageReferencesCommentSubstance()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockLlm = new Mock<ILlmClient>();

        string? capturedUserPrompt = null;
        var officerComment = "Water management schedule needs revision due to drought forecast in Division 4.";
        var generatedLlmMessage = "Your cultivation plan requires changes regarding your water management schedule for drought resilience.";

        mockLlm
            .Setup(l => l.CompleteJsonAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<LlmToolDefinition>>(),
                It.IsAny<Func<string, string, Task<string>>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, IReadOnlyList<LlmToolDefinition>, Func<string, string, Task<string>>, CancellationToken>(
                (_, userPrompt, _, _, _) => capturedUserPrompt = userPrompt)
            .ReturnsAsync(generatedLlmMessage);

        var plan = new CultivationPlan
        {
            Id = 102,
            CultivationCycleId = 1,
            RequestedByUserId = 43,
            Objective = "Standard Maha 2026",
            Status = PlanStatus.Rejected,
            OfficerComment = officerComment
        };
        context.CultivationPlans.Add(plan);
        await context.SaveChangesAsync();

        var service = new NotificationMessageService(
            context,
            mockLlm.Object,
            NullLogger<NotificationMessageService>.Instance);

        // Act
        var notification = await service.GenerateAndCreateCultivationPlanNotificationAsync(plan.Id);

        // Assert
        Assert.NotNull(notification);
        Assert.NotNull(capturedUserPrompt);
        Assert.Contains(officerComment, capturedUserPrompt);
        Assert.Equal(generatedLlmMessage, notification.Message);

        var log = await context.AgentRunLogs.FirstOrDefaultAsync(l => l.CultivationPlanId == plan.Id);
        Assert.NotNull(log);
        Assert.True(log.Success);
        Assert.Equal(generatedLlmMessage, log.RawOutput);
    }

    // 3. Safe-Failure Case: LLM call fails/times out -> fallback template used, Notification STILL created
    [Fact]
    public async Task CultivationPlan_GeminiFailsOrTimesOut_UsesFallbackTemplateAndStillCreatesNotificationAndRunLog()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockLlm = new Mock<ILlmClient>();

        mockLlm
            .Setup(l => l.CompleteJsonAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<LlmToolDefinition>>(),
                It.IsAny<Func<string, string, Task<string>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Gemini request timed out after 10s."));

        var plan = new CultivationPlan
        {
            Id = 103,
            CultivationCycleId = 1,
            RequestedByUserId = 44,
            Objective = "Standard Yala 2026",
            Status = PlanStatus.RevisionRequested,
            OfficerComment = "Please check the nursery seedling age."
        };
        context.CultivationPlans.Add(plan);
        await context.SaveChangesAsync();

        var service = new NotificationMessageService(
            context,
            mockLlm.Object,
            NullLogger<NotificationMessageService>.Instance);

        // Act
        var notification = await service.GenerateAndCreateCultivationPlanNotificationAsync(plan.Id);

        // Assert: Notification is STILL created using deterministic fallback template (never skip notifying the farmer)
        Assert.NotNull(notification);
        Assert.Equal(44, notification.UserId);
        var expectedFallback = $"Your cultivation plan status has been updated to {plan.Status}. Check the app for details.";
        Assert.Equal(expectedFallback, notification.Message);

        var dbNotification = await context.Notifications.FirstOrDefaultAsync(n => n.UserId == 44);
        Assert.NotNull(dbNotification);
        Assert.Equal(expectedFallback, dbNotification.Message);

        // Assert: AgentRunLog row created on fallback path with Success = false and Error recorded
        var log = await context.AgentRunLogs.FirstOrDefaultAsync(l => l.CultivationPlanId == plan.Id);
        Assert.NotNull(log);
        Assert.False(log.Success);
        Assert.Contains("timed out", log.Error);
    }

    // 4. Normal Case: PestDiseaseReport approved -> Notification created with generated message & DiagnosisRunLog
    [Fact]
    public async Task PestDiseaseReport_ApprovedReport_CreatesNotificationAndDiagnosisRunLog()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockLlm = new Mock<ILlmClient>();

        var expectedLlmMessage = "The agricultural officer has confirmed your report for Brown Planthopper. Please follow the recommended water management guidelines.";
        mockLlm
            .Setup(l => l.CompleteJsonAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<LlmToolDefinition>>(),
                It.IsAny<Func<string, string, Task<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedLlmMessage);

        var observation = new CropObservation
        {
            Id = 201,
            CultivationCycleId = 1,
            ReportedByUserId = 88,
            ObservationType = ObservationType.Pest,
            CropStage = GrowthStage.Tillering,
            Symptoms = "Yellowing lower leaves and hopper burn patches",
            Severity = ObservationSeverity.Moderate
        };
        context.CropObservations.Add(observation);

        var report = new PestDiseaseReport
        {
            Id = 301,
            CropObservationId = 201,
            PossibleIssue = "Brown Planthopper",
            Confidence = 0.92m,
            Status = PestDiseaseReportStatus.Approved,
            OfficerComment = "Confirmed BPH symptoms. Follow DOA management bulletin."
        };
        context.PestDiseaseReports.Add(report);
        await context.SaveChangesAsync();

        var service = new NotificationMessageService(
            context,
            mockLlm.Object,
            NullLogger<NotificationMessageService>.Instance);

        // Act
        var notification = await service.GenerateAndCreatePestDiseaseReportNotificationAsync(report.Id);

        // Assert
        Assert.NotNull(notification);
        Assert.Equal(88, notification.UserId);
        Assert.Equal("Pest & Disease Report Update", notification.Title);
        Assert.Equal(expectedLlmMessage, notification.Message);

        // Verify DiagnosisRunLog created on success
        var log = await context.DiagnosisRunLogs.FirstOrDefaultAsync(l => l.CropObservationId == observation.Id);
        Assert.NotNull(log);
        Assert.Equal("NotificationMessageAgent", log.AgentName);
        Assert.True(log.Success);
        Assert.Equal(expectedLlmMessage, log.RawOutput);
    }

    // 5. Safe-Failure Case: PestDiseaseReport Gemini fails -> uses fallback and still creates Notification
    [Fact]
    public async Task PestDiseaseReport_GeminiFails_UsesFallbackAndStillCreatesNotificationAndDiagnosisRunLog()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockLlm = new Mock<ILlmClient>();

        mockLlm
            .Setup(l => l.CompleteJsonAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<LlmToolDefinition>>(),
                It.IsAny<Func<string, string, Task<string>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Remote LLM connection failed"));

        var observation = new CropObservation
        {
            Id = 202,
            CultivationCycleId = 1,
            ReportedByUserId = 89,
            ObservationType = ObservationType.Disease,
            CropStage = GrowthStage.Flowering,
            Symptoms = "Spindle shaped lesions on leaf blades",
            Severity = ObservationSeverity.Severe
        };
        context.CropObservations.Add(observation);

        var report = new PestDiseaseReport
        {
            Id = 302,
            CropObservationId = 202,
            PossibleIssue = "Rice Blast",
            Confidence = 0.85m,
            Status = PestDiseaseReportStatus.RevisionRequested,
            OfficerComment = "Please upload a clearer picture of the leaf collars."
        };
        context.PestDiseaseReports.Add(report);
        await context.SaveChangesAsync();

        var service = new NotificationMessageService(
            context,
            mockLlm.Object,
            NullLogger<NotificationMessageService>.Instance);

        // Act
        var notification = await service.GenerateAndCreatePestDiseaseReportNotificationAsync(report.Id);

        // Assert
        Assert.NotNull(notification);
        Assert.Equal(89, notification.UserId);
        var expectedFallback = $"Your pest and disease report status has been updated to {report.Status}. Check the app for details.";
        Assert.Equal(expectedFallback, notification.Message);

        // Verify DiagnosisRunLog created with error
        var log = await context.DiagnosisRunLogs.FirstOrDefaultAsync(l => l.CropObservationId == observation.Id);
        Assert.NotNull(log);
        Assert.False(log.Success);
        Assert.Contains("connection failed", log.Error);
    }
}
