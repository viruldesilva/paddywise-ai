using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.Shared;
using PaddyWise.Backend.Tests.CropResource.Helpers;
using PaddyWise.Backend.Tests.FieldCultivation.Api;
using PaddyWise.Backend.Tests.FieldCultivation.Helpers;
using Xunit;

namespace PaddyWise.Backend.Tests.CropResource.Api;

/// <summary>
/// CropActivityAnalysisController end to end: running the Resource Analysis agent, the chat,
/// the officer's recommendation queue and review. The LLM is FieldCultivationApiFactory's
/// fake, which returns nothing, so the deterministic summary and answers are used.
/// </summary>
[Trait("Component", "CropResource")]
public class AnalysisApiTests
{
    private const string Farmer = "Farmer";
    private const string Officer = "AgriculturalOfficer";
    private const string FieldOfficer = "FieldOfficer";
    private const string Admin = "Admin";

    private static async Task<CultivationCycle> SeedCycleAsync(
        FieldCultivationApiFactory factory, int farmerId = 1, Division? division = null)
    {
        await using var db = factory.CreateDbContext();
        return await FcTestDb.SeedFarmerCycleAsync(db, farmerId, $"Farmer {farmerId}", division: division);
    }

    // ---------------------------------------------------------------- analysis

    [Fact]
    public async Task API09a_RunAnalysis_AsOwner_Returns200_AndQueuesPendingRecommendations()
    {
        using var factory = new FieldCultivationApiFactory();
        var cycle = await SeedCycleAsync(factory);

        using var farmer = factory.CreateClientAs(1, Farmer);
        var body = await ApiAssert.JsonAsync(
            await farmer.PostAsJsonAsync($"/api/cycles/{cycle.Id}/analysis", new { focusArea = "All" }), HttpStatusCode.OK);

        Assert.Equal(cycle.Id, body.GetProperty("fieldOverview").GetProperty("cycleId").GetInt32());
        Assert.Equal(3, body.GetProperty("recommendations").GetArrayLength());
        Assert.All(body.GetProperty("recommendations").EnumerateArray(),
            r => Assert.Equal("PENDING_OFFICER_REVIEW", r.GetProperty("status").GetString()));

        await using var db = factory.CreateDbContext();
        Assert.Equal(3, await db.CropActivityRecommendations.CountAsync(r => r.Status == "PENDING_OFFICER_REVIEW"));
    }

    [Fact]
    public async Task API09b_LatestAnalysis_And_Chat_Return200()
    {
        using var factory = new FieldCultivationApiFactory();
        var cycle = await SeedCycleAsync(factory);

        using var farmer = factory.CreateClientAs(1, Farmer);
        var latest = await ApiAssert.JsonAsync(await farmer.GetAsync($"/api/cycles/{cycle.Id}/analysis/latest"), HttpStatusCode.OK);
        Assert.Equal("Tillering", latest.GetProperty("fieldOverview").GetProperty("currentStage").GetString());

        var chat = await ApiAssert.JsonAsync(
            await farmer.PostAsJsonAsync($"/api/cycles/{cycle.Id}/ai-chat", new { question = "How much water now?" }), HttpStatusCode.OK);
        Assert.False(string.IsNullOrWhiteSpace(chat.GetProperty("answer").GetString()));
    }

    [Fact]
    public async Task API09c_AnalysisOfAnUnknownCycle_Returns404WithMessage()
    {
        using var factory = new FieldCultivationApiFactory();
        using var farmer = factory.CreateClientAs(1, Farmer);

        var response = await farmer.PostAsJsonAsync("/api/cycles/9999/analysis", new { });

        Assert.Equal("Cultivation cycle 9999 not found.", await ApiAssert.MessageAsync(response, HttpStatusCode.NotFound));
    }

    [Theory(Skip = "Known bug (finding 1): analysis, ai-chat, cycle recommendations and agent-audit check no ownership — any signed-in user reads (and, for analysis, writes recommendations to) another farmer's cycle.")]
    [InlineData("POST", "/analysis")]
    [InlineData("POST", "/ai-chat")]
    [InlineData("GET", "/recommendations")]
    [InlineData("GET", "/agent-audit")]
    public async Task API09d_AnotherFarmersCycle_Returns403(string method, string suffix)
    {
        using var factory = new FieldCultivationApiFactory();
        var cycle = await SeedCycleAsync(factory, farmerId: 2);

        using var farmerA = factory.CreateClientAs(1, Farmer);
        var response = await farmerA.SendAsync(new HttpRequestMessage(new HttpMethod(method), $"/api/cycles/{cycle.Id}{suffix}")
        {
            Content = method == "POST" ? JsonContent.Create(new { question = "Water?" }) : null
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------------------------------------------------------------- officer queue

    [Fact]
    public async Task API10a_PendingQueue_FiltersByDivisionAndStatus_PendingFirstThenNewest()
    {
        using var factory = new FieldCultivationApiFactory();
        int north;
        int oldPending, newPending, approved, southPending;
        await using (var db = factory.CreateDbContext())
        {
            var northDivision = await FcTestDb.SeedDivisionAsync(db, "North");
            var southDivision = await FcTestDb.SeedDivisionAsync(db, "South");
            north = northDivision.Id;
            var a = await FcTestDb.SeedFarmerCycleAsync(db, 1, "Farmer A", division: northDivision);
            var b = await FcTestDb.SeedFarmerCycleAsync(db, 2, "Farmer B", division: southDivision);
            var now = DateTime.UtcNow;
            oldPending = (await CrSeed.AddRecommendationAsync(db, a, 1, action: "old", createdAt: now.AddHours(-3))).Id;
            approved = (await CrSeed.AddRecommendationAsync(db, a, 1, status: "APPROVED", action: "approved", createdAt: now)).Id;
            newPending = (await CrSeed.AddRecommendationAsync(db, a, 1, action: "new", createdAt: now.AddHours(-1))).Id;
            southPending = (await CrSeed.AddRecommendationAsync(db, b, 2, action: "south", createdAt: now.AddHours(-2))).Id;
        }

        using var officer = factory.CreateClientAs(90, Officer);
        async Task<int[]> Ids(string query) =>
            (await ApiAssert.JsonAsync(await officer.GetAsync("/api/recommendations/pending" + query), HttpStatusCode.OK))
                .EnumerateArray().Select(x => x.GetProperty("id").GetInt32()).ToArray();

        // No status filter: everything, pending first, newest first within each group.
        Assert.Equal(new[] { newPending, southPending, oldPending, approved }, await Ids(""));
        Assert.Equal(new[] { newPending, oldPending, approved }, await Ids($"?divisionId={north}"));
        Assert.Equal(new[] { approved }, await Ids("?status=approved"));      // case-insensitive
        Assert.Equal(4, (await Ids("?status=ALL")).Length);
    }

    [Fact(Skip = "Known bug (finding 2): GET /api/recommendations/pending has no role check — a Farmer can read every farm's recommendation queue.")]
    public async Task API10b_PendingQueue_IsNotForFarmers()
    {
        using var factory = new FieldCultivationApiFactory();
        using var farmer = factory.CreateClientAs(1, Farmer);

        var response = await farmer.GetAsync("/api/recommendations/pending");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------------------------------------------------------------- reviews

    [Theory]
    [InlineData(90, Officer, HttpStatusCode.OK)]
    [InlineData(91, FieldOfficer, HttpStatusCode.OK)]
    [InlineData(92, Admin, HttpStatusCode.OK)]
    [InlineData(1, Farmer, HttpStatusCode.Forbidden)]
    public async Task API11a_OfficerReview_WhoMayReview(int userId, string role, HttpStatusCode expected)
    {
        using var factory = new FieldCultivationApiFactory();
        int recId;
        await using (var db = factory.CreateDbContext())
        {
            var cycle = await FcTestDb.SeedFarmerCycleAsync(db, 1, "Farmer A");
            await FcTestDb.SeedUserAsync(db, 90, "Officer", UserRole.AgriculturalOfficer);
            await FcTestDb.SeedUserAsync(db, 91, "Field Officer", UserRole.FieldOfficer);
            await FcTestDb.SeedUserAsync(db, 92, "Admin", UserRole.Admin);
            recId = (await CrSeed.AddRecommendationAsync(db, cycle, 1)).Id;
        }

        using var client = factory.CreateClientAs(userId, role);
        var response = await client.PostAsJsonAsync($"/api/recommendations/{recId}/officer-review",
            new { decision = "Approve", comment = "OK." });

        Assert.Equal(expected, response.StatusCode);
        if (expected == HttpStatusCode.OK)
            Assert.Equal("APPROVED", (await ApiAssert.JsonAsync(response, HttpStatusCode.OK)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task API11b_OfficerReview_OfAnUnknownRecommendation_Returns404WithMessage()
    {
        using var factory = new FieldCultivationApiFactory();
        await using (var db = factory.CreateDbContext())
            await FcTestDb.SeedUserAsync(db, 90, "Officer", UserRole.AgriculturalOfficer);

        using var officer = factory.CreateClientAs(90, Officer);
        var response = await officer.PostAsJsonAsync("/api/recommendations/9999/officer-review", new { decision = "Approve" });

        Assert.Equal("Recommendation #9999 not found.", await ApiAssert.MessageAsync(response, HttpStatusCode.NotFound));
    }

    [Fact]
    public async Task API11c_FarmerExecutesAnApprovedRecommendation_ThroughTheApi()
    {
        using var factory = new FieldCultivationApiFactory();
        string uid;
        int cycleId;
        await using (var db = factory.CreateDbContext())
        {
            var cycle = await FcTestDb.SeedFarmerCycleAsync(db, 1, "Farmer A");
            cycleId = cycle.Id;
            uid = (await CrSeed.AddRecommendationAsync(db, cycle, 1, status: "APPROVED")).RecommendationUid;
        }

        using var farmer = factory.CreateClientAs(1, Farmer);
        var body = await ApiAssert.JsonAsync(await farmer.PostAsJsonAsync($"/api/cycles/{cycleId}/recommendations/review",
            new { recommendationId = uid, decision = "Execute" }), HttpStatusCode.OK);

        Assert.Equal("EXECUTED", body.GetProperty("recommendation").GetProperty("status").GetString());
        Assert.True(body.GetProperty("createdActivityId").GetInt32() > 0);
    }
}
