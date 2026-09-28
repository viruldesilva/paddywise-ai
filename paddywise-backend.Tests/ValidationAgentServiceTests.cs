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

public class ValidationAgentServiceTests
{
    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
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

    [Fact]
    public async Task Case1_ReportWithKnownIssueAndValidConfidence_SucceedsAndSetsPendingReview()
    {
        // Case 1: A report with a PossibleIssue that exists in PestDiseaseKnowledgeEntries and valid Confidence -> IsValid true, Status moves to pending-review
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

        var runLog = await context.DiagnosisRunLogs.FirstOrDefaultAsync(l => l.AgentName == "ValidationAgent" && l.CropObservationId == 10);
        Assert.NotNull(runLog);
        Assert.True(runLog.Success);
    }

    [Fact]
    public async Task Case2_ReportWithUnknownIssue_FailsAndSetsRejected()
    {
        // Case 2: A report with a PossibleIssue NOT in the knowledge base -> IsValid false, Violations mentions the unrecognized issue, Status reflects failure
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

        var runLog = await context.DiagnosisRunLogs.FirstOrDefaultAsync(l => l.AgentName == "ValidationAgent" && l.CropObservationId == 20);
        Assert.NotNull(runLog);
        Assert.False(runLog.Success);
    }

    [Fact]
    public async Task Case3_ReportWithConfidenceOutsideRange_FailsValidation()
    {
        // Case 3: A report with Confidence outside 0.00-1.00 -> IsValid false
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
            Confidence = 1.45m, // Invalid: > 1.00
            Status = PestDiseaseReportStatus.PendingOfficerReview
        };
        context.PestDiseaseReports.Add(report);
        await context.SaveChangesAsync();

        var service = new ValidationAgentService(context, mockLlm.Object, NullLogger<ValidationAgentService>.Instance);

        var result = await service.ValidatePestDiseaseReportAsync(301);

        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Contains("Confidence score 1.45 is invalid"));

        var updatedReport = await context.PestDiseaseReports.FindAsync(301);
        Assert.NotNull(updatedReport);
        Assert.Equal(PestDiseaseReportStatus.Rejected, updatedReport.Status);
    }

    [Fact]
    public async Task Case4_PlanWithObviousDosageInSteps_FailsValidation()
    {
        // Case 4: A plan containing an obvious dosage value in its Steps -> IsValid false
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
            Id = 401,
            CultivationCycleId = 1,
            RequestedByUserId = 5,
            Objective = "High yield",
            PlanJson = JsonSerializer.Serialize(planOutput, CultivationPlanJson.Options),
            Status = PlanStatus.Draft
        };
        context.CultivationPlans.Add(plan);
        await context.SaveChangesAsync();

        var service = new ValidationAgentService(context, mockLlm.Object, NullLogger<ValidationAgentService>.Instance);

        var result = await service.ValidateCultivationPlanAsync(401);

        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Contains("prohibited numeric dosage ('50 kg')"));

        var updatedPlan = await context.CultivationPlans.FindAsync(401);
        Assert.NotNull(updatedPlan);
        Assert.Equal(PlanStatus.ValidationFailed, updatedPlan.Status);
        Assert.NotNull(updatedPlan.ValidationErrorsJson);
        Assert.NotNull(updatedPlan.OfficerComment);

        var runLog = await context.AgentRunLogs.FirstOrDefaultAsync(l => l.AgentName == "ValidationAgent" && l.CultivationPlanId == 401);
        Assert.NotNull(runLog);
        Assert.False(runLog.Success);
    }

    [Fact]
    public async Task Case5_LlmFailsOrTimesOutDuringExplanation_SavesResultWithFallback()
    {
        // Case 5: Gemini call for the explanation fails/times out during Step 3 -> the validation result still saves correctly, using the fallback text instead
        using var context = CreateInMemoryDbContext();
        var mockLlm = new Mock<ILlmClient>();
        mockLlm
            .Setup(l => l.CompleteJsonAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<LlmToolDefinition>>(),
                It.IsAny<Func<string, string, Task<string>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Gemini API call timed out."));

        var observation = new CropObservation
        {
            Id = 50,
            CultivationCycleId = 1,
            ReportedByUserId = 2,
            Symptoms = "Unknown symptoms"
        };
        context.CropObservations.Add(observation);

        var report = new PestDiseaseReport
        {
            Id = 501,
            CropObservationId = 50,
            PossibleIssue = "Unlisted Pest",
            Confidence = 0.70m,
            Status = PestDiseaseReportStatus.PendingOfficerReview
        };
        context.PestDiseaseReports.Add(report);
        await context.SaveChangesAsync();

        var service = new ValidationAgentService(context, mockLlm.Object, NullLogger<ValidationAgentService>.Instance);

        var result = await service.ValidatePestDiseaseReportAsync(501);

        Assert.False(result.IsValid);

        var updatedReport = await context.PestDiseaseReports.FindAsync(501);
        Assert.NotNull(updatedReport);
        Assert.Equal(PestDiseaseReportStatus.Rejected, updatedReport.Status);
        // Fallback text should contain the joined violations
        Assert.NotNull(updatedReport.OfficerComment);
        Assert.Contains("not found in the official pest/disease knowledge base", updatedReport.OfficerComment);

        var runLog = await context.DiagnosisRunLogs.FirstOrDefaultAsync(l => l.AgentName == "ValidationAgent" && l.CropObservationId == 50);
        Assert.NotNull(runLog);
        Assert.False(runLog.Success);
    }

    [Fact]
    public async Task Case6_ValidPlan_CreatesAgentRunLogEntryAndPendingApproval()
    {
        // Case 6: Verify DiagnosisRunLogs/AgentRunLogs entries are created for every run (here testing valid CultivationPlan)
        using var context = CreateInMemoryDbContext();
        var mockLlm = CreateMockLlm();

        var planOutput = new CultivationPlanOutput
        {
            Summary = "Standard agronomic timeline without dosages.",
            Steps = new List<PlanStep>
            {
                new()
                {
                    Stage = "Tillering",
                    Category = "Water",
                    Task = "Maintain shallow standing water",
                    Rationale = "Ensure weed suppression",
                    WindowStart = new DateOnly(2026, 5, 1),
                    WindowEnd = new DateOnly(2026, 5, 5)
                }
            },
            Assumptions = new List<string> { "Standard conditions" },
            Delegations = new List<PlanDelegation>()
        };

        var plan = new CultivationPlan
        {
            Id = 601,
            CultivationCycleId = 1,
            RequestedByUserId = 5,
            Objective = "Organic management",
            PlanJson = JsonSerializer.Serialize(planOutput, CultivationPlanJson.Options),
            Status = PlanStatus.Draft
        };
        context.CultivationPlans.Add(plan);
        await context.SaveChangesAsync();

        var service = new ValidationAgentService(context, mockLlm.Object, NullLogger<ValidationAgentService>.Instance);

        var result = await service.ValidateCultivationPlanAsync(601);

        Assert.True(result.IsValid);
        Assert.Empty(result.Violations);

        var updatedPlan = await context.CultivationPlans.FindAsync(601);
        Assert.NotNull(updatedPlan);
        Assert.Equal(PlanStatus.PendingOfficerApproval, updatedPlan.Status);
        Assert.Null(updatedPlan.ValidationErrorsJson);

        var runLog = await context.AgentRunLogs.FirstOrDefaultAsync(l => l.AgentName == "ValidationAgent" && l.CultivationPlanId == 601);
        Assert.NotNull(runLog);
        Assert.True(runLog.Success);
        Assert.Equal("ValidationAgent", runLog.AgentName);
    }
}
