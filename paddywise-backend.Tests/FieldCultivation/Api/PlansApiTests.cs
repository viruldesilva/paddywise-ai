using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Moq;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.Shared;
using PaddyWise.Backend.Tests.FieldCultivation.Helpers;
using Xunit;

namespace PaddyWise.Backend.Tests.FieldCultivation.Api;

/// <summary>
/// PlansController end to end with the real CultivationPlanningAgent and CultivationPlanService
/// behind it — only the LLM (FakePlanningLlmClient) and Component 4's second validation pass
/// (a mock) are replaced. Covers the request, the officer's queue and the review.
/// </summary>
[Trait("Component", "FieldCultivation")]
public class PlansApiTests
{
    private const string Farmer = "Farmer";
    private const string Officer = "AgriculturalOfficer";
    private const string Admin = "Admin";

    private static async Task<CultivationCycle> SeedCycleAsync(FieldCultivationApiFactory factory, int farmerId = 1)
    {
        await using var db = factory.CreateDbContext();
        return await FcTestDb.SeedFarmerCycleAsync(db, farmerId, $"Farmer {farmerId}");
    }

    /// <summary>The well-behaved model: both mandatory tools, then a plan copied from the timeline.</summary>
    private static void UseValidModel(FieldCultivationApiFactory factory, CultivationCycle cycle) =>
        factory.UseLlm(async (_, _, tools, _) =>
        {
            await FakePlanningLlmClient.CallRequiredToolsAsync(tools, cycle.Id);
            return PlanBuilder.Json(PlanBuilder.Valid(cycle));
        });

    // ---------------------------------------------------------------- request a plan

    [Fact]
    public async Task API15a_OwnerRequestsAPlan_Returns202Draft_ThenBecomesPendingOfficerApproval_WithTheAgentRun()
    {
        using var factory = new FieldCultivationApiFactory();
        var cycle = await SeedCycleAsync(factory);
        UseValidModel(factory, cycle);

        using var farmer = factory.CreateClientAs(1, Farmer);
        var response = await farmer.PostAsJsonAsync($"/api/cycles/{cycle.Id}/plans", new { objective = "Plan the rest of the season." });

        var body = await PlanPolling.AcceptedThenGeneratedAsync(farmer, response);
        Assert.Equal("PendingOfficerApproval", body.GetProperty("status").GetString());
        Assert.Equal(3, body.GetProperty("plan").GetProperty("steps").GetArrayLength());
        var run = Assert.Single(body.GetProperty("agentRuns").EnumerateArray(),
            r => r.GetProperty("agentName").GetString() == "CultivationPlanningAgent");
        Assert.True(run.GetProperty("success").GetBoolean());
        Assert.Equal(2, run.GetProperty("toolCalls").GetArrayLength());
        Assert.Single(factory.CapturedCalls);
    }

    [Fact]
    public async Task API15b_RequestPlan_AsOfficer403_OtherFarmer403_UnknownCycle404()
    {
        using var factory = new FieldCultivationApiFactory();
        var cycle = await SeedCycleAsync(factory, farmerId: 2);
        UseValidModel(factory, cycle);
        var body = new { objective = "Plan." };

        using var officer = factory.CreateClientAs(99, Officer);
        Assert.Equal(HttpStatusCode.Forbidden, (await officer.PostAsJsonAsync($"/api/cycles/{cycle.Id}/plans", body)).StatusCode);

        using var farmerA = factory.CreateClientAs(1, Farmer);
        await ApiAssert.MessageAsync(await farmerA.PostAsJsonAsync($"/api/cycles/{cycle.Id}/plans", body), HttpStatusCode.Forbidden);
        Assert.Equal("Cultivation cycle not found.",
            await ApiAssert.MessageAsync(await farmerA.PostAsJsonAsync("/api/cycles/9999/plans", body), HttpStatusCode.NotFound));

        Assert.Empty(factory.CapturedCalls);
    }

    [Fact]
    public async Task API15c_ASecondRequestWhileOneIsGeneratingOrPending_Returns400WithMessage()
    {
        using var factory = new FieldCultivationApiFactory();
        var cycle = await SeedCycleAsync(factory);
        UseValidModel(factory, cycle);
        using var farmer = factory.CreateClientAs(1, Farmer);
        var first = await farmer.PostAsJsonAsync($"/api/cycles/{cycle.Id}/plans", new { objective = "First." });

        // Straight away, while the first may still be a Draft...
        var whileGenerating = await farmer.PostAsJsonAsync($"/api/cycles/{cycle.Id}/plans", new { objective = "Second." });
        Assert.Contains("being generated, awaiting officer approval",
            await ApiAssert.MessageAsync(whileGenerating, HttpStatusCode.BadRequest));

        // ...and again once it is waiting for the officer.
        await PlanPolling.AcceptedThenGeneratedAsync(farmer, first);
        var whilePending = await farmer.PostAsJsonAsync($"/api/cycles/{cycle.Id}/plans", new { objective = "Third." });
        Assert.Contains("awaiting officer approval", await ApiAssert.MessageAsync(whilePending, HttpStatusCode.BadRequest));
        Assert.Single(factory.CapturedCalls);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1000, true)]
    [InlineData(1001, false)]
    public async Task API16_ObjectiveLength_RequiredAndAtMost1000(int length, bool expectedValid)
    {
        using var factory = new FieldCultivationApiFactory();
        var cycle = await SeedCycleAsync(factory);
        UseValidModel(factory, cycle);
        using var farmer = factory.CreateClientAs(1, Farmer);

        var response = await farmer.PostAsJsonAsync($"/api/cycles/{cycle.Id}/plans", new { objective = new string('o', length) });

        if (expectedValid)
            await PlanPolling.AcceptedThenGeneratedAsync(farmer, response);
        else
            await ApiAssert.ModelErrorsAsync(response);
    }

    [Fact]
    public async Task API17a_ComponentFoursSecondPass_RunsForAPlanThatPassedPassOne()
    {
        using var factory = new FieldCultivationApiFactory();
        var cycle = await SeedCycleAsync(factory);
        UseValidModel(factory, cycle);
        using var farmer = factory.CreateClientAs(1, Farmer);

        var body = await PlanPolling.AcceptedThenGeneratedAsync(
            farmer, await farmer.PostAsJsonAsync($"/api/cycles/{cycle.Id}/plans", new { objective = "Plan." }));

        factory.ValidationAgent.Verify(
            v => v.ValidateCultivationPlanAsync(body.GetProperty("id").GetInt32(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task API17b_ComponentFoursSecondPass_NeverRunsOnAPlanPassOneFailed()
    {
        using var factory = new FieldCultivationApiFactory();
        var cycle = await SeedCycleAsync(factory);
        factory.UseLlm(async (_, _, tools, _) =>
        {
            await FakePlanningLlmClient.CallRequiredToolsAsync(tools, cycle.Id);
            return PlanBuilder.Json(PlanBuilder.Valid(cycle, nutrientTask: "Apply 50 kg urea."));
        });
        using var farmer = factory.CreateClientAs(1, Farmer);

        var body = await PlanPolling.AcceptedThenGeneratedAsync(
            farmer, await farmer.PostAsJsonAsync($"/api/cycles/{cycle.Id}/plans", new { objective = "Plan." }));

        Assert.Equal("ValidationFailed", body.GetProperty("status").GetString());
        Assert.Contains("dosage decision out of scope", body.GetProperty("validationErrors")[0].GetString());
        factory.ValidationAgent.Verify(
            v => v.ValidateCultivationPlanAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task API17c_AtStartup_ARecentDraftIsGeneratedAgain_AndAStaleDraftIsFailed()
    {
        using var factory = new FieldCultivationApiFactory();
        CultivationCycle cycle;
        CultivationPlan recent, stale;
        // Seeded before the host starts, as if left over from a restart mid-run.
        await using (var db = factory.CreateDbContext())
        {
            cycle = await FcTestDb.SeedFarmerCycleAsync(db, 1, "Farmer 1");
            recent = await FcTestDb.SeedPlanAsync(db, cycle, 1, PlanStatus.Draft, DateTime.UtcNow.AddMinutes(-5));
            stale = await FcTestDb.SeedPlanAsync(db, cycle, 1, PlanStatus.Draft, DateTime.UtcNow.AddHours(-2));
        }
        UseValidModel(factory, cycle);

        using var farmer = factory.CreateClientAs(1, Farmer);

        var regenerated = await PlanPolling.WaitUntilGeneratedAsync(farmer, recent.Id);
        Assert.Equal("PendingOfficerApproval", regenerated.GetProperty("status").GetString());
        var failed = await PlanPolling.WaitUntilGeneratedAsync(farmer, stale.Id);
        Assert.Equal("ValidationFailed", failed.GetProperty("status").GetString());
        Assert.Contains("interrupted", failed.GetProperty("validationErrors")[0].GetString());
        Assert.False(Assert.Single(failed.GetProperty("agentRuns").EnumerateArray()).GetProperty("success").GetBoolean());
    }

    // ---------------------------------------------------------------- officer queue

    [Theory]
    [InlineData(Farmer, HttpStatusCode.Forbidden)]
    [InlineData(Admin, HttpStatusCode.Forbidden)]
    [InlineData(Officer, HttpStatusCode.OK)]
    public async Task API18a_PendingQueue_IsForAgriculturalOfficersOnly(string role, HttpStatusCode expected)
    {
        using var factory = new FieldCultivationApiFactory();
        using var client = factory.CreateClientAs(1, role);

        Assert.Equal(expected, (await client.GetAsync("/api/plans/pending")).StatusCode);
    }

    [Fact]
    public async Task API18b_PendingQueue_OnlyPendingPlans_NewestFirst_FilteredByDivision()
    {
        using var factory = new FieldCultivationApiFactory();
        int north, south;
        await using (var db = factory.CreateDbContext())
        {
            var northDivision = await FcTestDb.SeedDivisionAsync(db, "North");
            var southDivision = await FcTestDb.SeedDivisionAsync(db, "South");
            north = northDivision.Id;
            south = southDivision.Id;
            var a = await FcTestDb.SeedFarmerCycleAsync(db, 1, "Farmer A", division: northDivision);
            var b = await FcTestDb.SeedFarmerCycleAsync(db, 2, "Farmer B", division: southDivision);
            var now = DateTime.UtcNow;
            await FcTestDb.SeedPlanAsync(db, a, 1, PlanStatus.PendingOfficerApproval, now.AddHours(-2), "older north");
            await FcTestDb.SeedPlanAsync(db, b, 2, PlanStatus.PendingOfficerApproval, now.AddHours(-1), "newer south");
            await FcTestDb.SeedPlanAsync(db, a, 1, PlanStatus.ValidationFailed, now, "failed");
            await FcTestDb.SeedPlanAsync(db, b, 2, PlanStatus.Approved, now, "approved");
        }

        using var officer = factory.CreateClientAs(99, Officer);

        var all = await ApiAssert.JsonAsync(await officer.GetAsync("/api/plans/pending"), HttpStatusCode.OK);
        Assert.Equal(new[] { "newer south", "older north" }, all.EnumerateArray().Select(p => p.GetProperty("objective").GetString()));
        Assert.Equal("Farmer B", all[0].GetProperty("farmerName").GetString());
        Assert.Equal("Maha", all[0].GetProperty("season").GetString());

        var onlyNorth = await ApiAssert.JsonAsync(await officer.GetAsync($"/api/plans/pending?divisionId={north}"), HttpStatusCode.OK);
        Assert.Equal("older north", Assert.Single(onlyNorth.EnumerateArray()).GetProperty("objective").GetString());

        var onlySouth = await ApiAssert.JsonAsync(await officer.GetAsync($"/api/plans/pending?divisionId={south}"), HttpStatusCode.OK);
        Assert.Equal("newer south", Assert.Single(onlySouth.EnumerateArray()).GetProperty("objective").GetString());
    }

    // ---------------------------------------------------------------- review

    private static async Task<(int PlanId, int CycleId)> SeedPendingAsync(
        FieldCultivationApiFactory factory, CycleStatus cycleStatus = CycleStatus.Planned, PlanStatus planStatus = PlanStatus.PendingOfficerApproval)
    {
        await using var db = factory.CreateDbContext();
        var cycle = await FcTestDb.SeedFarmerCycleAsync(db, 1, "QA Farmer", sownDaysAgo: -5, status: cycleStatus);
        await FcTestDb.SeedUserAsync(db, 99, "Officer", UserRole.AgriculturalOfficer);
        var plan = await FcTestDb.SeedPlanAsync(db, cycle, 1, planStatus);
        return (plan.Id, cycle.Id);
    }

    [Theory]
    [InlineData("Approve", null, "Approved", CycleStatus.Active)]
    [InlineData("Reject", "Wrong variety for this soil.", "Rejected", CycleStatus.Planned)]
    [InlineData("RequestRevision", "Add a water step at flowering.", "RevisionRequested", CycleStatus.Planned)]
    public async Task API19a_EachDecision_SetsThePlanAndCycleStatus(
        string decision, string? comment, string expectedPlan, CycleStatus expectedCycle)
    {
        using var factory = new FieldCultivationApiFactory();
        var (planId, cycleId) = await SeedPendingAsync(factory);
        using var officer = factory.CreateClientAs(99, Officer);

        var body = await ApiAssert.JsonAsync(
            await officer.PostAsJsonAsync($"/api/plans/{planId}/review", new { decision, comment }), HttpStatusCode.OK);

        Assert.Equal(expectedPlan, body.GetProperty("status").GetString());
        await using var db = factory.CreateDbContext();
        Assert.Equal(expectedCycle, (await db.CultivationCycles.SingleAsync(c => c.Id == cycleId)).Status);
    }

    [Theory]
    [InlineData("Reject", "A comment is required when rejecting a plan.")]
    [InlineData("RequestRevision", "A comment is required when asking for a revision.")]
    [InlineData("Escalate", "Decision must be Approve, Reject or RequestRevision.")]
    public async Task API19b_ReviewRules_Return400WithMessage(string decision, string message)
    {
        using var factory = new FieldCultivationApiFactory();
        var (planId, _) = await SeedPendingAsync(factory);
        using var officer = factory.CreateClientAs(99, Officer);

        var response = await officer.PostAsJsonAsync($"/api/plans/{planId}/review", new { decision });

        Assert.Equal(message, await ApiAssert.MessageAsync(response, HttpStatusCode.BadRequest));
    }

    [Fact]
    public async Task API19c_ReviewingAnAlreadyReviewedPlan_Returns400()
    {
        using var factory = new FieldCultivationApiFactory();
        var (planId, _) = await SeedPendingAsync(factory, planStatus: PlanStatus.Approved);
        using var officer = factory.CreateClientAs(99, Officer);

        var response = await officer.PostAsJsonAsync($"/api/plans/{planId}/review", new { decision = "Approve" });

        Assert.Contains("only a plan awaiting officer approval", await ApiAssert.MessageAsync(response, HttpStatusCode.BadRequest));
    }

    [Theory]
    [InlineData(Farmer)]
    [InlineData(Admin)]
    public async Task API19d_OnlyAnAgriculturalOfficerCanReview(string role)
    {
        using var factory = new FieldCultivationApiFactory();
        var (planId, _) = await SeedPendingAsync(factory);
        using var client = factory.CreateClientAs(1, role);

        var response = await client.PostAsJsonAsync($"/api/plans/{planId}/review", new { decision = "Approve" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(1000, true)]
    [InlineData(1001, false)]
    public async Task API19e_CommentLength_AtMost1000(int length, bool expectedValid)
    {
        using var factory = new FieldCultivationApiFactory();
        var (planId, _) = await SeedPendingAsync(factory);
        using var officer = factory.CreateClientAs(99, Officer);

        var response = await officer.PostAsJsonAsync($"/api/plans/{planId}/review",
            new { decision = "Reject", comment = new string('c', length) });

        if (expectedValid)
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        else
            Assert.Contains("Comment cannot exceed 1000 characters.", await ApiAssert.ModelErrorsAsync(response));
    }

    [Fact]
    public async Task API19f_ReviewingAnUnknownPlan_Returns404()
    {
        using var factory = new FieldCultivationApiFactory();
        using var officer = factory.CreateClientAs(99, Officer);

        var response = await officer.PostAsJsonAsync("/api/plans/9999/review", new { decision = "Approve" });

        Assert.Equal("Cultivation plan not found.", await ApiAssert.MessageAsync(response, HttpStatusCode.NotFound));
    }

    // ---------------------------------------------------------------- read

    [Fact]
    public async Task API20_ReadingPlans_OtherFarmer403_Officer200_WithAgentRuns()
    {
        using var factory = new FieldCultivationApiFactory();
        var cycle = await SeedCycleAsync(factory, farmerId: 2);
        UseValidModel(factory, cycle);
        using var owner = factory.CreateClientAs(2, Farmer);
        var created = await PlanPolling.AcceptedThenGeneratedAsync(
            owner, await owner.PostAsJsonAsync($"/api/cycles/{cycle.Id}/plans", new { objective = "Plan." }));
        var planId = created.GetProperty("id").GetInt32();

        using var farmerA = factory.CreateClientAs(1, Farmer);
        await ApiAssert.MessageAsync(await farmerA.GetAsync($"/api/plans/{planId}"), HttpStatusCode.Forbidden);
        await ApiAssert.MessageAsync(await farmerA.GetAsync($"/api/cycles/{cycle.Id}/plans"), HttpStatusCode.Forbidden);

        using var officer = factory.CreateClientAs(99, Officer);
        var single = await ApiAssert.JsonAsync(await officer.GetAsync($"/api/plans/{planId}"), HttpStatusCode.OK);
        Assert.NotEqual(0, single.GetProperty("agentRuns").GetArrayLength());
        Assert.Equal("get_cycle", single.GetProperty("agentRuns")[0].GetProperty("toolCalls")[0].GetProperty("tool").GetString());

        var list = await ApiAssert.JsonAsync(await officer.GetAsync($"/api/cycles/{cycle.Id}/plans"), HttpStatusCode.OK);
        Assert.Equal(planId, Assert.Single(list.EnumerateArray()).GetProperty("id").GetInt32());

        Assert.Equal(HttpStatusCode.NotFound, (await officer.GetAsync("/api/plans/9999")).StatusCode);
    }
}
