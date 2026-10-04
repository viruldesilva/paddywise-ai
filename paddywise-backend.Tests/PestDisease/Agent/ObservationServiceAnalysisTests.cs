using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PaddyWise.Api.Agents.Shared;
using PaddyWise.Api.DTOs.PestDisease;
using PaddyWise.Api.Entities.PestDisease;
using PaddyWise.Api.Services.PestDisease;
using PaddyWise.Backend.Tests.PestDisease.Helpers;
using Xunit;

namespace PaddyWise.Backend.Tests.PestDisease.Agent;

/// <summary>
/// ObservationService.RequestAnalysisAsync's own logic — approval enforcement, failure
/// logging/handling, and the business rules around who can request analysis when. The
/// diagnosis agent itself is mocked directly here (not the full CropAnalysisAgent +
/// ILlmClient chain — see CropAnalysisAgentTests.cs for that), since these tests are about
/// what ObservationService does with the agent's result, not how the agent produces it.
/// </summary>
[Trait("Component", "PestDisease")]
public class ObservationServiceAnalysisTests
{
    private const int FarmerId = 1;
    private const int OtherFarmerId = 2;

    private static ObservationService CreateService(
        PaddyWise.Api.Data.ApplicationDbContext context, Mock<IAgent<DelegatedTask, DelegatedTaskResult>> mockAgent)
    {
        mockAgent.Setup(a => a.Name).Returns(AgentNames.PestDiseaseDiagnosis);
        return new ObservationService(
            context, mockAgent.Object, Mock.Of<IPhotoStorageService>(), NullLogger<ObservationService>.Instance);
    }

    [Fact]
    public async Task AG10_SuccessfulRun_SavesEveryReportAsPendingOfficerReview_NeverApproved()
    {
        using var context = TestDbFactory.CreateContext();
        var cycle = await TestDbFactory.SeedFarmerCycleAsync(context, FarmerId, "QA Farmer");
        var observation = await TestDbFactory.SeedObservationAsync(context, cycle, FarmerId);

        var agentOutput = new CropAnalysisAgentOutput
        {
            PossibleIssues = new List<PossibleIssueCandidate>
            {
                new() { Name = "Rice Blast", Confidence = 0.8m, Source = "Sri Lanka Department of Agriculture" },
                new() { Name = "Brown Spot", Confidence = 0.4m, Source = "Sri Lanka Department of Agriculture" }
            },
            RecommendedNextStep = "Officer review recommended"
        };

        var mockAgent = new Mock<IAgent<DelegatedTask, DelegatedTaskResult>>();
        mockAgent
            .Setup(a => a.RunAsync(It.IsAny<DelegatedTask>(), It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentResult<DelegatedTaskResult>
            {
                Success = true,
                Output = new DelegatedTaskResult
                {
                    Note = "Identified 2 possible match(es).",
                    ResultJson = System.Text.Json.JsonSerializer.Serialize(agentOutput)
                },
                Duration = TimeSpan.FromSeconds(1)
            });

        var service = CreateService(context, mockAgent);
        var result = await service.RequestAnalysisAsync(observation.Id, FarmerId);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Reports.Count);
        Assert.All(result.Reports, r => Assert.Equal("PendingOfficerReview", r.Status));

        var savedReports = context.PestDiseaseReports.Where(r => r.CropObservationId == observation.Id).ToList();
        Assert.Equal(2, savedReports.Count);
        Assert.All(savedReports, r => Assert.Equal(PestDiseaseReportStatus.PendingOfficerReview, r.Status));
    }

    [Fact]
    public async Task AG11a_LlmExceptionFromAgent_LogsFailedRunAndReturnsHandledError()
    {
        using var context = TestDbFactory.CreateContext();
        var cycle = await TestDbFactory.SeedFarmerCycleAsync(context, FarmerId, "QA Farmer");
        var observation = await TestDbFactory.SeedObservationAsync(context, cycle, FarmerId);

        var mockAgent = new Mock<IAgent<DelegatedTask, DelegatedTaskResult>>();
        mockAgent
            .Setup(a => a.RunAsync(It.IsAny<DelegatedTask>(), It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LlmException(HttpStatusCode.TooManyRequests, "rate limited"));

        var service = CreateService(context, mockAgent);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RequestAnalysisAsync(observation.Id, FarmerId));
        Assert.NotNull(exception.Message);

        var runLog = context.DiagnosisRunLogs.FirstOrDefault(l => l.CropObservationId == observation.Id);
        Assert.NotNull(runLog);
        Assert.False(runLog!.Success);
        Assert.NotNull(runLog.Error);
    }

    [Fact]
    public async Task AG11b_TimeoutFromAgent_LogsFailedRunAndReturnsHandledError()
    {
        using var context = TestDbFactory.CreateContext();
        var cycle = await TestDbFactory.SeedFarmerCycleAsync(context, FarmerId, "QA Farmer");
        var observation = await TestDbFactory.SeedObservationAsync(context, cycle, FarmerId);

        var mockAgent = new Mock<IAgent<DelegatedTask, DelegatedTaskResult>>();
        mockAgent
            .Setup(a => a.RunAsync(It.IsAny<DelegatedTask>(), It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException("the HttpClient timed out"));

        var service = CreateService(context, mockAgent);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RequestAnalysisAsync(observation.Id, FarmerId));
        Assert.NotNull(exception.Message);

        var runLog = context.DiagnosisRunLogs.FirstOrDefault(l => l.CropObservationId == observation.Id);
        Assert.NotNull(runLog);
        Assert.False(runLog!.Success);
        Assert.NotNull(runLog.Error);
    }

    [Fact]
    public async Task AG12a_ObservationAlreadyHasReports_RequestIsRejectedWithoutCallingTheAgent()
    {
        using var context = TestDbFactory.CreateContext();
        var cycle = await TestDbFactory.SeedFarmerCycleAsync(context, FarmerId, "QA Farmer");
        var observation = await TestDbFactory.SeedObservationAsync(context, cycle, FarmerId);

        context.PestDiseaseReports.Add(new PestDiseaseReport
        {
            CropObservationId = observation.Id,
            PossibleIssue = "Rice Blast",
            Confidence = 0.8m,
            Status = PestDiseaseReportStatus.PendingOfficerReview
        });
        await context.SaveChangesAsync();

        var mockAgent = new Mock<IAgent<DelegatedTask, DelegatedTaskResult>>();
        var service = CreateService(context, mockAgent);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RequestAnalysisAsync(observation.Id, FarmerId));

        mockAgent.Verify(
            a => a.RunAsync(It.IsAny<DelegatedTask>(), It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AG12b_AnotherFarmerRequestingAnalysisOnMyObservation_IsRejected()
    {
        using var context = TestDbFactory.CreateContext();
        var cycle = await TestDbFactory.SeedFarmerCycleAsync(context, FarmerId, "QA Farmer");
        var observation = await TestDbFactory.SeedObservationAsync(context, cycle, FarmerId);

        var mockAgent = new Mock<IAgent<DelegatedTask, DelegatedTaskResult>>();
        var service = CreateService(context, mockAgent);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.RequestAnalysisAsync(observation.Id, OtherFarmerId));

        mockAgent.Verify(
            a => a.RunAsync(It.IsAny<DelegatedTask>(), It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
