using System.Text.Json;
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
public class RevisionDraftServiceTests
{
    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task DraftPlanRevisionCommentAsync_WithRealValidationErrors_GeneratesNonEmptyDraft()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockLlm = new Mock<ILlmClient>();

        var expectedDraft = "Please adjust the sowing date. Step 2 sowing date is before the land prep stage.";
        mockLlm
            .Setup(l => l.CompleteJsonAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<LlmToolDefinition>>(),
                It.IsAny<Func<string, string, Task<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDraft);

        var cycle = new CultivationCycle
        {
            Id = 10,
            FieldId = 1,
            VarietyId = 1,
            Season = Season.Yala,
            Year = 2026,
            Field = new Field { Id = 1, Name = "Test Field", Area = 5.0m, SoilType = "Clay", IrrigationType = "Irrigated" },
            Variety = new Variety { Id = 1, Name = "Bg 300", DurationDays = 105 }
        };
        context.CultivationCycles.Add(cycle);

        var plan = new CultivationPlan
        {
            Id = 1,
            CultivationCycleId = 10,
            RequestedByUserId = 20,
            Objective = "Increase yield while minimizing fertilizer",
            ValidationErrorsJson = JsonSerializer.Serialize(new[] { "Step 2 sowing date precedes land prep window" }),
            Status = PlanStatus.ValidationFailed
        };
        context.CultivationPlans.Add(plan);
        await context.SaveChangesAsync();

        var service = new RevisionDraftService(
            context,
            mockLlm.Object,
            NullLogger<RevisionDraftService>.Instance);

        // Act
        var result = await service.DraftPlanRevisionCommentAsync(plan.Id);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(result));
        Assert.Equal(expectedDraft, result);
        mockLlm.Verify(l => l.CompleteJsonAsync(
            It.IsAny<string>(),
            It.Is<string>(prompt => prompt.Contains("Step 2 sowing date precedes land prep window")),
            It.IsAny<IReadOnlyList<LlmToolDefinition>>(),
            It.IsAny<Func<string, string, Task<string>>>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify AgentRunLog row created on success path
        var log = await context.AgentRunLogs.FirstOrDefaultAsync(l => l.CultivationPlanId == plan.Id);
        Assert.NotNull(log);
        Assert.Equal("RevisionDraftAgent", log.AgentName);
        Assert.True(log.Success);
        Assert.Equal(expectedDraft, log.RawOutput);
        Assert.Null(log.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("[]")]
    public async Task DraftPlanRevisionCommentAsync_WithNullOrEmptyValidationErrors_ReturnsFallbackWithoutCallingGemini(string? validationErrors)
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockLlm = new Mock<ILlmClient>();

        var plan = new CultivationPlan
        {
            Id = 2,
            CultivationCycleId = 10,
            RequestedByUserId = 20,
            Objective = "Standard rice cultivation",
            ValidationErrorsJson = validationErrors,
            Status = PlanStatus.PendingOfficerApproval
        };
        context.CultivationPlans.Add(plan);
        await context.SaveChangesAsync();

        var service = new RevisionDraftService(
            context,
            mockLlm.Object,
            NullLogger<RevisionDraftService>.Instance);

        // Act
        var result = await service.DraftPlanRevisionCommentAsync(plan.Id);

        // Assert
        Assert.Equal(RevisionDraftService.DefaultPlanFallback, result);
        mockLlm.Verify(l => l.CompleteJsonAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<IReadOnlyList<LlmToolDefinition>>(),
            It.IsAny<Func<string, string, Task<string>>>(),
            It.IsAny<CancellationToken>()), Times.Never);

        mockLlm.Verify(l => l.CompleteJsonWithImagesAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<IReadOnlyList<LlmImagePart>>(),
            It.IsAny<IReadOnlyList<LlmToolDefinition>>(),
            It.IsAny<Func<string, string, Task<string>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DraftPlanRevisionCommentAsync_WhenGeminiThrowsOrTimesOut_ReturnsFallbackAndLogsFailure()
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

        var cycle = new CultivationCycle
        {
            Id = 10,
            FieldId = 1,
            VarietyId = 1,
            Season = Season.Yala,
            Year = 2026,
            Field = new Field { Id = 1, Name = "Test Field", Area = 5.0m, SoilType = "Clay", IrrigationType = "Irrigated" },
            Variety = new Variety { Id = 1, Name = "Bg 300", DurationDays = 105 }
        };
        context.CultivationCycles.Add(cycle);

        var plan = new CultivationPlan
        {
            Id = 3,
            CultivationCycleId = 10,
            RequestedByUserId = 20,
            Objective = "Yield maximization",
            ValidationErrorsJson = JsonSerializer.Serialize(new[] { "Invalid dosage for urea" }),
            Status = PlanStatus.ValidationFailed
        };
        context.CultivationPlans.Add(plan);
        await context.SaveChangesAsync();

        var service = new RevisionDraftService(
            context,
            mockLlm.Object,
            NullLogger<RevisionDraftService>.Instance);

        // Act
        var result = await service.DraftPlanRevisionCommentAsync(plan.Id);

        // Assert - must return fallback rather than throwing
        Assert.Equal(RevisionDraftService.DefaultPlanFallback, result);

        // Verify AgentRunLog row created with Success = false and error populated
        var log = await context.AgentRunLogs.FirstOrDefaultAsync(l => l.CultivationPlanId == plan.Id);
        Assert.NotNull(log);
        Assert.Equal("RevisionDraftAgent", log.AgentName);
        Assert.False(log.Success);
        Assert.Contains("timed out", log.Error);
    }

    [Fact]
    public async Task DraftReportRevisionCommentAsync_WithObservation_GeneratesDraftAndLogsToDiagnosisRunLogs()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockLlm = new Mock<ILlmClient>();

        var expectedDraft = "Please upload a clearer close-up photograph of the leaf damage to confirm whether this is Brown Planthopper.";
        mockLlm
            .Setup(l => l.CompleteJsonAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<LlmToolDefinition>>(),
                It.IsAny<Func<string, string, Task<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDraft);

        var observation = new CropObservation
        {
            Id = 100,
            CultivationCycleId = 1,
            ReportedByUserId = 5,
            ObservationType = ObservationType.Pest,
            CropStage = GrowthStage.Tillering,
            Symptoms = "Yellowing leaves with small hopper bugs visible near water line",
            Severity = ObservationSeverity.Moderate
        };
        context.CropObservations.Add(observation);

        var report = new PestDiseaseReport
        {
            Id = 50,
            CropObservationId = 100,
            PossibleIssue = "Brown Planthopper",
            Confidence = 0.85m,
            Status = PestDiseaseReportStatus.PendingOfficerReview
        };
        context.PestDiseaseReports.Add(report);
        await context.SaveChangesAsync();

        var service = new RevisionDraftService(
            context,
            mockLlm.Object,
            NullLogger<RevisionDraftService>.Instance);

        // Act
        var result = await service.DraftReportRevisionCommentAsync(report.Id);

        // Assert
        Assert.Equal(expectedDraft, result);

        var log = await context.DiagnosisRunLogs.FirstOrDefaultAsync(l => l.CropObservationId == observation.Id);
        Assert.NotNull(log);
        Assert.Equal("RevisionDraftAgent", log.AgentName);
        Assert.True(log.Success);
        Assert.Equal(expectedDraft, log.RawOutput);
    }

    [Fact]
    public async Task DraftReportRevisionCommentAsync_WhenGeminiFails_ReturnsFallbackAndLogsFailure()
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
            .ThrowsAsync(new InvalidOperationException("Gemini quota exceeded"));

        var observation = new CropObservation
        {
            Id = 101,
            CultivationCycleId = 1,
            ReportedByUserId = 5,
            ObservationType = ObservationType.Disease,
            CropStage = GrowthStage.Tillering,
            Symptoms = "Brown spots on leaves",
            Severity = ObservationSeverity.Severe
        };
        context.CropObservations.Add(observation);

        var report = new PestDiseaseReport
        {
            Id = 51,
            CropObservationId = 101,
            PossibleIssue = "Brown Spot Disease",
            Confidence = 0.75m,
            Status = PestDiseaseReportStatus.PendingOfficerReview
        };
        context.PestDiseaseReports.Add(report);
        await context.SaveChangesAsync();

        var service = new RevisionDraftService(
            context,
            mockLlm.Object,
            NullLogger<RevisionDraftService>.Instance);

        // Act
        var result = await service.DraftReportRevisionCommentAsync(report.Id);

        // Assert
        Assert.Equal(RevisionDraftService.DefaultReportFallback, result);

        var log = await context.DiagnosisRunLogs.FirstOrDefaultAsync(l => l.CropObservationId == observation.Id);
        Assert.NotNull(log);
        Assert.Equal("RevisionDraftAgent", log.AgentName);
        Assert.False(log.Success);
        Assert.Contains("quota exceeded", log.Error);
    }
}
