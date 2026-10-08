using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PaddyWise.Api.Agents.FieldCultivation;
using PaddyWise.Api.Agents.Shared;
using PaddyWise.Api.Data;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.PestDisease;
using PaddyWise.Api.Services.ReportingApproval.Agents;
using Xunit;

namespace PaddyWise.Backend.Tests;

[Trait("Component", "ReportingApproval")]
public class ValidationAgentServiceTests
{
    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "ValidationTestDb_" + Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Mock<ILlmClient> CreateMockLlm(string defaultResponse = "Explanation of validation issues.")
    {
        var mockLlm = new Mock<ILlmClient>();
        mockLlm
            .Setup(l => l.CompleteJsonAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<LlmToolDefinition>>(),
                It.IsAny<Func<string, string, Task<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultResponse);

        return mockLlm;
    }

    // 1. Normal Case: Valid PlanJson / PossibleIssue+Confidence -> IsValid true, status updated correctly
    [Fact]
    public async Task ValidatePestDiseaseReport_ValidIssueAndConfidence_ReturnsValidAndSetsPendingOfficerReview()
    {
        using var context = CreateInMemoryDbContext();
        var mockLlm = CreateMockLlm();

        context.PestDiseaseKnowledgeEntries.Add(new PestDiseaseKnowledge
        {
            Id = 1,
            Name = "Brown Planthopper",
            Symptoms = "Yellowing and drying of leaves"
        });

        var observation = new CropObservation
        {
            Id = 10,
            CultivationCycleId = 1,
            ReportedByUserId = 2,
            Symptoms = "Yellowing plants"
        };
        context.CropObservations.Add(observation);

        var report = new PestDiseaseReport
        {
            Id = 101,
            CropObservationId = 10,
            PossibleIssue = "Brown Planthopper",
            Confidence = 0.88m,
            Status = PestDiseaseReportStatus.PendingOfficerReview
        };
        context.PestDiseaseReports.Add(report);
        await context.SaveChangesAsync();

        var service = new ValidationAgentService(context, mockLlm.Object, NullLogger<ValidationAgentService>.Instance);

        var result = await service.ValidatePestDiseaseReportAsync(101);

        Assert.True(result.IsValid);
        Assert.Empty(result.Violations);
        Assert.True(result.RequiresOfficerReview);

        var updatedReport = await context.PestDiseaseReports.FindAsync(101);
        Assert.NotNull(updatedReport);
        Assert.Equal(PestDiseaseReportStatus.PendingOfficerReview, updatedReport.Status);

        // Observability check: run log created on success
        var runLog = await context.DiagnosisRunLogs.FirstOrDefaultAsync(l => l.AgentName == "ValidationAgent" && l.CropObservationId == 10);
        Assert.NotNull(runLog);
        Assert.True(runLog.Success);
    }

    // 2. Invalid Case: PossibleIssue not found in knowledge base -> IsValid false, violation recorded, status Rejected
    [Fact]
    public async Task ValidatePestDiseaseReport_UnknownIssueNotInKnowledgeBase_ReturnsInvalidAndSetsRejected()
    {
        using var context = CreateInMemoryDbContext();
        var mockLlm = CreateMockLlm("This diagnosis was rejected because the identified pest does not exist in the official knowledge base.");

        context.PestDiseaseKnowledgeEntries.Add(new PestDiseaseKnowledge
        {
            Id = 1,
            Name = "Brown Planthopper"
        });

        var observation = new CropObservation
        {
            Id = 20,
            CultivationCycleId = 1,
            ReportedByUserId = 2,
            Symptoms = "Unknown spots"
        };
        context.CropObservations.Add(observation);

        var report = new PestDiseaseReport
        {
            Id = 201,
            CropObservationId = 20,
            PossibleIssue = "Hallucinated Martian Pest",
            Confidence = 0.95m,
            Status = PestDiseaseReportStatus.PendingOfficerReview
        };
        context.PestDiseaseReports.Add(report);
        await context.SaveChangesAsync();

        var service = new ValidationAgentService(context, mockLlm.Object, NullLogger<ValidationAgentService>.Instance);

        var result = await service.ValidatePestDiseaseReportAsync(201);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Violations);
        Assert.Contains(result.Violations, v => v.Contains("not found in the official pest/disease knowledge base"));

        var updatedReport = await context.PestDiseaseReports.FindAsync(201);
        Assert.NotNull(updatedReport);
        Assert.Equal(PestDiseaseReportStatus.Rejected, updatedReport.Status);
        Assert.NotNull(updatedReport.OfficerComment);

        // Observability check: run log created on failure
        var runLog = await context.DiagnosisRunLogs.FirstOrDefaultAsync(l => l.AgentName == "ValidationAgent" && l.CropObservationId == 20);
        Assert.NotNull(runLog);
        Assert.False(runLog.Success);
        Assert.Contains("not found", runLog.Error);
    }

    // 3. Boundary Case: Confidence outside 0.00-1.00 -> IsValid false
    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(1.50)]
    public async Task ValidatePestDiseaseReport_ConfidenceOutsideRange_ReturnsInvalid(decimal invalidConfidence)
    {
        using var context = CreateInMemoryDbContext();
        var mockLlm = CreateMockLlm();

        context.PestDiseaseKnowledgeEntries.Add(new PestDiseaseKnowledge
        {
            Id = 1,
            Name = "Bacterial Leaf Blight"
        });

        var observation = new CropObservation
        {
            Id = 30,
            CultivationCycleId = 1,
            ReportedByUserId = 2,
            Symptoms = "Wilting"
        };
        context.CropObservations.Add(observation);

        var report = new PestDiseaseReport
        {
            Id = 301,
            CropObservationId = 30,
            PossibleIssue = "Bacterial Leaf Blight",
            Confidence = invalidConfidence,
            Status = PestDiseaseReportStatus.PendingOfficerReview
        };
        context.PestDiseaseReports.Add(report);
        await context.SaveChangesAsync();

        var service = new ValidationAgentService(context, mockLlm.Object, NullLogger<ValidationAgentService>.Instance);

        var result = await service.ValidatePestDiseaseReportAsync(301);

        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Contains("is invalid. It must be between 0.00 and 1.00"));

        var updatedReport = await context.PestDiseaseReports.FindAsync(301);
        Assert.NotNull(updatedReport);
        Assert.Equal(PestDiseaseReportStatus.Rejected, updatedReport.Status);
    }

    // 4. Boundary Edge Case: Confidence exactly 0.00 and exactly 1.00 -> IsValid true
    [Theory]
    [InlineData(0.00)]
    [InlineData(1.00)]
    public async Task ValidatePestDiseaseReport_ConfidenceExactlyAtBounds_ReturnsValid(decimal boundaryConfidence)
    {
        using var context = CreateInMemoryDbContext();
        var mockLlm = CreateMockLlm();

        context.PestDiseaseKnowledgeEntries.Add(new PestDiseaseKnowledge
        {
            Id = 1,
            Name = "Rice Blast"
        });

        var observation = new CropObservation
        {
            Id = 40,
            CultivationCycleId = 1,
            ReportedByUserId = 2,
            Symptoms = "Lesions"
        };
        context.CropObservations.Add(observation);

        var report = new PestDiseaseReport
        {
            Id = 401,
            CropObservationId = 40,
            PossibleIssue = "Rice Blast",
            Confidence = boundaryConfidence,
            Status = PestDiseaseReportStatus.PendingOfficerReview
        };
        context.PestDiseaseReports.Add(report);
        await context.SaveChangesAsync();

        var service = new ValidationAgentService(context, mockLlm.Object, NullLogger<ValidationAgentService>.Instance);

        var result = await service.ValidatePestDiseaseReportAsync(401);

        Assert.True(result.IsValid);
        Assert.Empty(result.Violations);
        Assert.True(result.RequiresOfficerReview);

        var updatedReport = await context.PestDiseaseReports.FindAsync(401);
        Assert.NotNull(updatedReport);
        Assert.Equal(PestDiseaseReportStatus.PendingOfficerReview, updatedReport.Status);
    }

    // 5. Business-Rule Violation Case: PlanJson containing a dosage/numeric value -> IsValid false
    [Fact]
    public async Task ValidateCultivationPlan_PlanContainingNumericDosage_ReturnsInvalidAndSetsValidationFailed()
    {
        using var context = CreateInMemoryDbContext();
        var mockLlm = CreateMockLlm("This plan failed because the planning agent specified numeric input dosages.");

        var planOutput = new CultivationPlanOutput
        {
            Summary = "Cultivation schedule for Bg 300.",
            Steps = new List<PlanStep>
            {
                new()
                {
                    Stage = "Tillering",
                    Category = "Nutrient",
                    Task = "Apply 50 kg of urea per acre",
                    Rationale = "Promote vegetative growth",
                    WindowStart = new DateOnly(2026, 5, 1),
                    WindowEnd = new DateOnly(2026, 5, 5)
                }
            },
            Assumptions = new List<string> { "Standard rainfall" },
            Delegations = new List<PlanDelegation>()
        };

        var plan = new CultivationPlan
        {
            Id = 501,
            CultivationCycleId = 1,
            RequestedByUserId = 5,
            Objective = "High yield",
            PlanJson = JsonSerializer.Serialize(planOutput, CultivationPlanJson.Options),
            Status = PlanStatus.Draft
        };
        context.CultivationPlans.Add(plan);
        await context.SaveChangesAsync();

        var service = new ValidationAgentService(context, mockLlm.Object, NullLogger<ValidationAgentService>.Instance);

        var result = await service.ValidateCultivationPlanAsync(501);

        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Contains("prohibited numeric dosage ('50 kg')"));

        var updatedPlan = await context.CultivationPlans.FindAsync(501);
        Assert.NotNull(updatedPlan);
        Assert.Equal(PlanStatus.ValidationFailed, updatedPlan.Status);
        Assert.NotNull(updatedPlan.ValidationErrorsJson);
        Assert.NotNull(updatedPlan.OfficerComment);

        // Observability check: run log created on failure path
        var runLog = await context.AgentRunLogs.FirstOrDefaultAsync(l => l.AgentName == "ValidationAgent" && l.CultivationPlanId == 501);
        Assert.NotNull(runLog);
        Assert.False(runLog.Success);
    }

    // 6. Failure Case: Malformed/null PlanJson -> handled gracefully without throwing
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{ invalid json string }")]
    [InlineData("{}")]
    [InlineData("[]")]
    public async Task ValidateCultivationPlan_MalformedOrNullPlanJson_HandledGracefullyWithoutThrowing(string? malformedJson)
    {
        using var context = CreateInMemoryDbContext();
        var mockLlm = CreateMockLlm("Plan JSON is corrupt or missing.");

        var plan = new CultivationPlan
        {
            Id = 601,
            CultivationCycleId = 1,
            RequestedByUserId = 5,
            Objective = "Test graceful error handling",
            PlanJson = malformedJson,
            Status = PlanStatus.Draft
        };
        context.CultivationPlans.Add(plan);
        await context.SaveChangesAsync();

        var service = new ValidationAgentService(context, mockLlm.Object, NullLogger<ValidationAgentService>.Instance);

        // Act - must not throw
        var exception = await Record.ExceptionAsync(() => service.ValidateCultivationPlanAsync(601));
        Assert.Null(exception);

        // Verify result is invalid
        var planInDb = await context.CultivationPlans.FindAsync(601);
        Assert.NotNull(planInDb);
        Assert.Equal(PlanStatus.ValidationFailed, planInDb.Status);
        Assert.NotNull(planInDb.ValidationErrorsJson);
    }

    // 7. Observability Check: Verify AgentRunLogs / DiagnosisRunLogs created on both success and failure paths
    [Fact]
    public async Task Observability_AgentRunLogsAndDiagnosisRunLogs_CreatedOnBothSuccessAndFailurePaths()
    {
        using var context = CreateInMemoryDbContext();
        var mockLlm = CreateMockLlm();

        // 7a. Valid plan -> Success AgentRunLog
        var validPlanOutput = new CultivationPlanOutput
        {
            Summary = "Clean plan without dosages.",
            Steps = new List<PlanStep>
            {
                new()
                {
                    Stage = "Tillering",
                    Category = "Water",
                    Task = "Maintain shallow standing water",
                    Rationale = "Weed control",
                    WindowStart = new DateOnly(2026, 5, 1),
                    WindowEnd = new DateOnly(2026, 5, 5)
                }
            },
            Assumptions = new List<string> { "Standard conditions" },
            Delegations = new List<PlanDelegation>()
        };

        var validPlan = new CultivationPlan
        {
            Id = 701,
            CultivationCycleId = 1,
            RequestedByUserId = 5,
            Objective = "Clean plan",
            PlanJson = JsonSerializer.Serialize(validPlanOutput, CultivationPlanJson.Options),
            Status = PlanStatus.Draft
        };
        context.CultivationPlans.Add(validPlan);

        // 7b. Invalid plan -> Failure AgentRunLog
        var invalidPlan = new CultivationPlan
        {
            Id = 702,
            CultivationCycleId = 1,
            RequestedByUserId = 5,
            Objective = "Invalid plan",
            PlanJson = "{}",
            Status = PlanStatus.Draft
        };
        context.CultivationPlans.Add(invalidPlan);
        await context.SaveChangesAsync();

        var service = new ValidationAgentService(context, mockLlm.Object, NullLogger<ValidationAgentService>.Instance);

        // Execute both
        await service.ValidateCultivationPlanAsync(701);
        await service.ValidateCultivationPlanAsync(702);

        // Assert logs exist
        var logSuccess = await context.AgentRunLogs.FirstOrDefaultAsync(l => l.CultivationPlanId == 701);
        Assert.NotNull(logSuccess);
        Assert.True(logSuccess.Success);
        Assert.Equal("ValidationAgent", logSuccess.AgentName);

        var logFailure = await context.AgentRunLogs.FirstOrDefaultAsync(l => l.CultivationPlanId == 702);
        Assert.NotNull(logFailure);
        Assert.False(logFailure.Success);
        Assert.NotNull(logFailure.Error);
        Assert.Equal("ValidationAgent", logFailure.AgentName);
    }
}
