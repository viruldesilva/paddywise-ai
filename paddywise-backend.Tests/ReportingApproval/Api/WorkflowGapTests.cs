using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PaddyWise.Api.Agents.Shared;
using PaddyWise.Api.DTOs.PestDisease;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.Shared;
using PaddyWise.Api.Services.PestDisease;
using PaddyWise.Api.Services.ReportingApproval.Agents;
using PaddyWise.Backend.Tests.FieldCultivation.Api;
using PaddyWise.Backend.Tests.FieldCultivation.Helpers;
using PaddyWise.Backend.Tests.PestDisease.Helpers;
using PaddyWise.Backend.Tests.ReportingApproval.Helpers;
using Xunit;

namespace PaddyWise.Backend.Tests.ReportingApproval.Api;

/// <summary>
/// Whether Component 4's services are actually wired into the workflows they were written
/// for: notifications after an officer's decision, the pest-report validation pass, and the
/// real second validation pass on a requested plan.
/// </summary>
[Trait("Component", "ReportingApproval")]
public class WorkflowGapTests
{
    [Fact(Skip = "Known bug (finding 1): NotificationMessageService is never called by production code — an officer approving, rejecting or sending back a plan creates no notification for the farmer.")]
    public async Task RAAPI06_AnOfficersPlanDecision_NotifiesTheFarmer()
    {
        using var factory = new FieldCultivationApiFactory();
        int planId;
        await using (var db = factory.CreateDbContext())
        {
            var cycle = await FcTestDb.SeedFarmerCycleAsync(db, 1, "QA Farmer", sownDaysAgo: -5, status: CycleStatus.Planned);
            await FcTestDb.SeedUserAsync(db, 90, "Officer", UserRole.AgriculturalOfficer);
            planId = (await FcTestDb.SeedPlanAsync(db, cycle, 1, PlanStatus.PendingOfficerApproval)).Id;
        }

        using var officer = factory.CreateClientAs(90, "AgriculturalOfficer");
        var response = await officer.PostAsJsonAsync($"/api/plans/{planId}/review", new { decision = "Approve" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var check = factory.CreateDbContext();
        var notification = Assert.Single(await check.Notifications.Where(n => n.UserId == 1).ToListAsync());
        Assert.Equal("Approved", notification.Status);
    }

    [Fact(Skip = "Known bug (finding 2): ValidatePestDiseaseReportAsync is never called — reports from the pest & disease agent never get Component 4's validation pass (no ValidationAgent DiagnosisRunLog).")]
    public async Task RAAPI07_PestAnalysis_RunsComponentFoursValidationPass()
    {
        using var db = FcTestDb.CreateContext();
        await TestDbFactory.SeedKnowledgeBaseAsync(db);
        var cycle = await FcTestDb.SeedFarmerCycleAsync(db, 1, "QA Farmer");
        var observation = await TestDbFactory.SeedObservationAsync(db, cycle, 1);

        // The pest & disease agent itself is mocked, exactly as PestDisease's own service tests do.
        var agent = new Mock<IAgent<DelegatedTask, DelegatedTaskResult>>();
        agent.Setup(a => a.Name).Returns(AgentNames.PestDiseaseDiagnosis);
        agent.Setup(a => a.RunAsync(It.IsAny<DelegatedTask>(), It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentResult<DelegatedTaskResult>
            {
                Success = true,
                Output = new DelegatedTaskResult
                {
                    Note = "1 match",
                    ResultJson = System.Text.Json.JsonSerializer.Serialize(new CropAnalysisAgentOutput
                    {
                        PossibleIssues = new() { new() { Name = "Rice Blast", Confidence = 0.8m, Source = "DOA" } },
                        RecommendedNextStep = "Officer review"
                    })
                }
            });
        var service = new ObservationService(db, agent.Object, Mock.Of<IPhotoStorageService>(), NullLogger<ObservationService>.Instance);

        await service.RequestAnalysisAsync(observation.Id, 1);

        Assert.True(await db.DiagnosisRunLogs.AnyAsync(l => l.AgentName == ValidationAgentService.AgentName));
    }

    [Fact]
    public async Task RAAPI08_TheRealSecondPass_FailsAPlanThePlanningValidatorPassed()
    {
        using var factory = new RaApiFactory();
        CultivationCycle cycle;
        await using (var db = factory.CreateDbContext())
            cycle = await FcTestDb.SeedFarmerCycleAsync(db, 1, "QA Farmer");

        // Component 1's validator does not scan the summary; Component 4's does.
        factory.UseLlm(async (system, _, tools, _) =>
        {
            if (system.Contains("Validation Agent"))
                return "The summary states a dosage.";
            await FakePlanningLlmClient.CallRequiredToolsAsync(tools, cycle.Id);
            return PlanBuilder.Json(PlanBuilder.Valid(cycle) with { Summary = "Apply 50 kg urea across the season." });
        });

        using var farmer = factory.CreateClientAs(1, "Farmer");
        var body = await PlanPolling.AcceptedThenGeneratedAsync(
            farmer, await farmer.PostAsJsonAsync($"/api/cycles/{cycle.Id}/plans", new { objective = "Good yield." }));

        Assert.Equal("ValidationFailed", body.GetProperty("status").GetString());
        Assert.Contains("Plan summary contains prohibited numeric dosage: '50 kg'",
            body.GetProperty("validationErrors")[0].GetString());
        var runs = body.GetProperty("agentRuns").EnumerateArray().Select(r => r.GetProperty("agentName").GetString()).ToList();
        Assert.Contains("CultivationPlanningAgent", runs);
        Assert.Contains(ValidationAgentService.AgentName, runs);
    }
}
