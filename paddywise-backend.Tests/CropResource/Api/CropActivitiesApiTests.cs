using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PaddyWise.Api.Entities.CropResource;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.Shared;
using PaddyWise.Backend.Tests.CropResource.Helpers;
using PaddyWise.Backend.Tests.FieldCultivation.Api;
using PaddyWise.Backend.Tests.FieldCultivation.Helpers;
using Xunit;

namespace PaddyWise.Backend.Tests.CropResource.Api;

/// <summary>
/// CropActivitiesController end to end through the real pipeline, using Component 1's
/// FieldCultivationApiFactory (InMemory database, header-driven TestAuthHandler) and
/// ApiAssert, unchanged.
/// </summary>
[Trait("Component", "CropResource")]
public class CropActivitiesApiTests
{
    private const string Farmer = "Farmer";
    private const string Officer = "AgriculturalOfficer";
    private const string FieldOfficer = "FieldOfficer";
    private const string Admin = "Admin";

    private static object Body(CropActivityType type, DateOnly date, string details) => new
    {
        activityType = type.ToString(),
        date = date.ToString("yyyy-MM-dd"),
        detailsJson = details
    };

    private static async Task<CultivationCycle> SeedCycleAsync(
        FieldCultivationApiFactory factory, int farmerId = 1, int sownDaysAgo = 20)
    {
        await using var db = factory.CreateDbContext();
        return await FcTestDb.SeedFarmerCycleAsync(db, farmerId, $"Farmer {farmerId}", sownDaysAgo);
    }

    private static async Task<CropActivity> SeedActivityAsync(
        FieldCultivationApiFactory factory, CultivationCycle cycle, int loggedBy, int daysAgo = 1,
        CropActivityType type = CropActivityType.Irrigation, string? details = null, DateTimeOffset? createdAt = null)
    {
        await using var db = factory.CreateDbContext();
        return await CrSeed.AddActivityAsync(db, cycle, loggedBy, type, CrSeed.Today.AddDays(-daysAgo),
            details ?? CrSeed.Irrigation(), createdAt);
    }

    private static async Task SeedStaffAsync(FieldCultivationApiFactory factory)
    {
        await using var db = factory.CreateDbContext();
        await FcTestDb.SeedUserAsync(db, 90, "Officer", UserRole.AgriculturalOfficer);
        await FcTestDb.SeedUserAsync(db, 91, "Field Officer", UserRole.FieldOfficer);
        await FcTestDb.SeedUserAsync(db, 92, "Admin", UserRole.Admin);
    }

    // ---------------------------------------------------------------- access

    [Theory]
    [InlineData("GET", "/api/cycles/1/activities")]
    [InlineData("GET", "/api/activities")]
    [InlineData("POST", "/api/cycles/1/analysis")]
    [InlineData("GET", "/api/recommendations/pending")]
    public async Task API01_NoLogin_Returns401(string method, string url)
    {
        using var factory = new FieldCultivationApiFactory();
        using var client = factory.CreateClient();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = method == "POST" ? JsonContent.Create(new { }) : null
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(1, Farmer, HttpStatusCode.OK)]
    [InlineData(90, Officer, HttpStatusCode.OK)]
    [InlineData(91, FieldOfficer, HttpStatusCode.OK)]
    [InlineData(92, Admin, HttpStatusCode.Forbidden)]   // not in the endpoint's roles
    public async Task API02a_CycleActivities_WhoMayRead(int userId, string role, HttpStatusCode expected)
    {
        using var factory = new FieldCultivationApiFactory();
        var cycle = await SeedCycleAsync(factory);
        await SeedStaffAsync(factory);
        await SeedActivityAsync(factory, cycle, 1);

        using var client = factory.CreateClientAs(userId, role);
        var response = await client.GetAsync($"/api/cycles/{cycle.Id}/activities");

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task API02b_CycleActivities_NewestDateFirst_ThenNewestLogged()
    {
        using var factory = new FieldCultivationApiFactory();
        var cycle = await SeedCycleAsync(factory);
        var now = DateTimeOffset.UtcNow;
        var older = await SeedActivityAsync(factory, cycle, 1, daysAgo: 5);
        var sameDayEarlier = await SeedActivityAsync(factory, cycle, 1, daysAgo: 1, createdAt: now.AddHours(-2));
        var sameDayLater = await SeedActivityAsync(factory, cycle, 1, daysAgo: 1, createdAt: now);

        using var farmer = factory.CreateClientAs(1, Farmer);
        var body = await ApiAssert.JsonAsync(await farmer.GetAsync($"/api/cycles/{cycle.Id}/activities"), HttpStatusCode.OK);

        Assert.Equal(new[] { sameDayLater.Id, sameDayEarlier.Id, older.Id },
            body.EnumerateArray().Select(a => a.GetProperty("id").GetInt32()));
        Assert.Equal("Irrigation", body[0].GetProperty("activityType").GetString());
        Assert.Equal("Farmer 1", body[0].GetProperty("loggedByUserName").GetString());
    }

    [Fact(Skip = "Known bug (finding 3): GET /api/cycles/{id}/activities has no try/catch — another farmer's cycle (UnauthorizedAccessException) and an unknown cycle (InvalidOperationException) both surface as 500.")]
    public async Task API02c_CycleActivities_OtherFarmer403_UnknownCycle404()
    {
        using var factory = new FieldCultivationApiFactory();
        var cycle = await SeedCycleAsync(factory, farmerId: 2);

        using var farmerA = factory.CreateClientAs(1, Farmer);
        await ApiAssert.MessageAsync(await farmerA.GetAsync($"/api/cycles/{cycle.Id}/activities"), HttpStatusCode.Forbidden);
        await ApiAssert.MessageAsync(await farmerA.GetAsync("/api/cycles/9999/activities"), HttpStatusCode.NotFound);
    }

    // ---------------------------------------------------------------- create

    [Fact]
    public async Task API03a_CreateActivity_AsOwner_Returns201()
    {
        using var factory = new FieldCultivationApiFactory();
        var cycle = await SeedCycleAsync(factory);

        using var farmer = factory.CreateClientAs(1, Farmer);
        var response = await farmer.PostAsJsonAsync($"/api/cycles/{cycle.Id}/activities", new
        {
            activityType = "fertilizer", // case-insensitive enum
            date = CrSeed.Today.ToString("yyyy-MM-dd"),
            detailsJson = CrSeed.Fertilizer("Urea", 50)
        });

        var body = await ApiAssert.JsonAsync(response, HttpStatusCode.Created);
        Assert.Equal("Fertilizer", body.GetProperty("activityType").GetString());
        Assert.Equal(1, body.GetProperty("loggedByUserId").GetInt32());
        Assert.Equal("Farmer 1", body.GetProperty("loggedByUserName").GetString());
    }

    [Fact]
    public async Task API03b_CreateActivity_Officer403_OtherFarmer403_UnknownCycle404()
    {
        using var factory = new FieldCultivationApiFactory();
        var cycle = await SeedCycleAsync(factory, farmerId: 2);
        var body = Body(CropActivityType.Irrigation, CrSeed.Today, CrSeed.Irrigation());

        using var officer = factory.CreateClientAs(90, Officer);
        Assert.Equal(HttpStatusCode.Forbidden, (await officer.PostAsJsonAsync($"/api/cycles/{cycle.Id}/activities", body)).StatusCode);

        using var farmerA = factory.CreateClientAs(1, Farmer);
        Assert.Equal(HttpStatusCode.Forbidden, (await farmerA.PostAsJsonAsync($"/api/cycles/{cycle.Id}/activities", body)).StatusCode);
        Assert.Equal("Cultivation cycle not found.",
            await ApiAssert.MessageAsync(await farmerA.PostAsJsonAsync("/api/cycles/9999/activities", body), HttpStatusCode.NotFound));
    }

    [Fact(Skip = "Known bug (finding 10): the controller answers a refused farmer with Forbid(), a 403 with an empty body — CLAUDE.md requires { message } on every 4xx.")]
    public async Task API03c_ARefusedFarmer_GetsA403WithAMessage()
    {
        using var factory = new FieldCultivationApiFactory();
        var cycle = await SeedCycleAsync(factory, farmerId: 2);

        using var farmerA = factory.CreateClientAs(1, Farmer);
        var response = await farmerA.PostAsJsonAsync($"/api/cycles/{cycle.Id}/activities",
            Body(CropActivityType.Irrigation, CrSeed.Today, CrSeed.Irrigation()));

        await ApiAssert.MessageAsync(response, HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("today", true, null)]
    [InlineData("today-7", true, null)]
    [InlineData("today-8", false, "cannot be older than the past week")]
    [InlineData("today+1", false, "Activity date cannot be in the future.")]
    [InlineData("sowing-1", false, "cannot be earlier than the cultivation cycle's sowing date")]
    [InlineData("harvest+1", false, "cannot be after the harvest date")]
    public async Task API04_ActivityDateWindow(string when, bool valid, string? message)
    {
        using var factory = new FieldCultivationApiFactory();
        // A cycle sown 3 days ago, so "the day before sowing" is still inside the 7-day window.
        var sownDaysAgo = when == "sowing-1" ? 3 : 20;
        var cycle = await SeedCycleAsync(factory, sownDaysAgo: sownDaysAgo);
        if (when == "harvest+1")
        {
            await using var db = factory.CreateDbContext();
            var tracked = await db.CultivationCycles.FindAsync(cycle.Id);
            tracked!.ActualHarvestDate = CrSeed.Today.AddDays(-3);
            tracked.Status = CycleStatus.Harvested;
            await db.SaveChangesAsync();
        }

        var date = when switch
        {
            "today" => CrSeed.Today,
            "today-7" => CrSeed.Today.AddDays(-7),
            "today-8" => CrSeed.Today.AddDays(-8),
            "today+1" => CrSeed.Today.AddDays(1),
            "sowing-1" => cycle.SowingDate.AddDays(-1),
            _ => CrSeed.Today.AddDays(-2) // the day after the actual harvest
        };

        using var farmer = factory.CreateClientAs(1, Farmer);
        var response = await farmer.PostAsJsonAsync($"/api/cycles/{cycle.Id}/activities",
            Body(CropActivityType.Irrigation, date, CrSeed.Irrigation()));

        if (valid)
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        else
            Assert.Contains(message!, await ApiAssert.MessageAsync(response, HttpStatusCode.BadRequest));
    }

    public static IEnumerable<object?[]> DetailCases() => new List<object?[]>
    {
        // Fertilizer
        new object?[] { CropActivityType.Fertilizer, "{\"quantity\":50,\"cropStage\":\"Tillering\",\"region\":\"Dry\",\"method\":\"Broadcast\"}", "Fertilizer type is required." },
        new object?[] { CropActivityType.Fertilizer, CrSeed.Fertilizer(quantity: 0), "Fertilizer quantity must be a positive number greater than 0." },
        new object?[] { CropActivityType.Fertilizer, CrSeed.Fertilizer(quantity: 0.01), null },
        new object?[] { CropActivityType.Fertilizer, CrSeed.Fertilizer(quantity: "12.5"), null }, // numeric string
        new object?[] { CropActivityType.Fertilizer, CrSeed.Fertilizer(cropStage: ""), "Crop stage is required." },
        new object?[] { CropActivityType.Fertilizer, CrSeed.Fertilizer(region: " "), "Climatic region/zone is required." },
        new object?[] { CropActivityType.Fertilizer, CrSeed.Fertilizer(method: ""), "Application method is required." },
        // Irrigation
        new object?[] { CropActivityType.Irrigation, CrSeed.Irrigation(waterLevel: -0.01), "Water level must be a non-negative number." },
        new object?[] { CropActivityType.Irrigation, CrSeed.Irrigation(waterLevel: 0), null },
        new object?[] { CropActivityType.Irrigation, CrSeed.Irrigation(duration: 0), "Duration must be a positive number greater than 0." },
        new object?[] { CropActivityType.Irrigation, CrSeed.Irrigation(duration: 0.01), null },
        new object?[] { CropActivityType.Irrigation, CrSeed.Irrigation(source: ""), "Water source is required." },
        // Pesticide
        new object?[] { CropActivityType.Pesticide, CrSeed.Pesticide(product: ""), "Product name is required." },
        new object?[] { CropActivityType.Pesticide, CrSeed.Pesticide(targetPest: ""), "Target pest/disease is required." },
        new object?[] { CropActivityType.Pesticide, CrSeed.Pesticide(quantity: 0), "Pesticide quantity must be a positive number greater than 0." },
        new object?[] { CropActivityType.Pesticide, CrSeed.Pesticide(method: ""), "Application method is required." },
        // Other
        new object?[] { CropActivityType.Other, CrSeed.Other(specificActivity: ""), "Specific activity type is required." },
        new object?[] { CropActivityType.Other, CrSeed.Other("Bund repair"), null },
        // Any type
        new object?[] { CropActivityType.Irrigation, "{ not json", "Invalid JSON format for activity details." },
        // Blank details never reach the service: [Required] on DetailsJson rejects them first ({ errors }).
        new object?[] { CropActivityType.Other, "   ", ModelValidation },
    };

    /// <summary>Marks a case answered by ASP.NET model validation ({ errors }), not the service ({ message }).</summary>
    private const string ModelValidation = "<model validation>";

    [Theory]
    [MemberData(nameof(DetailCases))]
    public async Task API05_ActivityDetails_AreValidatedPerType(CropActivityType type, string details, string? message)
    {
        using var factory = new FieldCultivationApiFactory();
        var cycle = await SeedCycleAsync(factory);

        using var farmer = factory.CreateClientAs(1, Farmer);
        var response = await farmer.PostAsJsonAsync($"/api/cycles/{cycle.Id}/activities", Body(type, CrSeed.Today, details));

        if (message == null)
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        else if (message == ModelValidation)
            await ApiAssert.ModelErrorsAsync(response);
        else
            Assert.Equal(message, await ApiAssert.MessageAsync(response, HttpStatusCode.BadRequest));
    }

    // ---------------------------------------------------------------- update and delete

    [Theory]
    [InlineData(1, Farmer, HttpStatusCode.OK)]
    [InlineData(92, Admin, HttpStatusCode.OK)]
    [InlineData(90, Officer, HttpStatusCode.Forbidden)]
    [InlineData(2, Farmer, HttpStatusCode.Forbidden)]
    public async Task API06a_UpdateActivity_WhoMayEdit(int userId, string role, HttpStatusCode expected)
    {
        using var factory = new FieldCultivationApiFactory();
        var cycle = await SeedCycleAsync(factory);
        var activity = await SeedActivityAsync(factory, cycle, 1);

        using var client = factory.CreateClientAs(userId, role);
        var response = await client.PutAsJsonAsync($"/api/activities/{activity.Id}",
            Body(CropActivityType.Irrigation, CrSeed.Today, CrSeed.Irrigation(5, 6, "Tank")));

        Assert.Equal(expected, response.StatusCode);
        if (expected == HttpStatusCode.OK)
            Assert.Contains("Tank", (await ApiAssert.JsonAsync(response, HttpStatusCode.OK)).GetProperty("detailsJson").GetString());
    }

    [Theory]
    [InlineData(1, Farmer, HttpStatusCode.NoContent)]
    [InlineData(92, Admin, HttpStatusCode.NoContent)]
    [InlineData(90, Officer, HttpStatusCode.Forbidden)]
    [InlineData(2, Farmer, HttpStatusCode.Forbidden)]
    public async Task API06b_DeleteActivity_WhoMayDelete(int userId, string role, HttpStatusCode expected)
    {
        using var factory = new FieldCultivationApiFactory();
        var cycle = await SeedCycleAsync(factory);
        var activity = await SeedActivityAsync(factory, cycle, 1);

        using var client = factory.CreateClientAs(userId, role);
        var response = await client.DeleteAsync($"/api/activities/{activity.Id}");

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task API06c_UpdateOrDeleteAnUnknownActivity_Returns404WithMessage()
    {
        using var factory = new FieldCultivationApiFactory();
        using var farmer = factory.CreateClientAs(1, Farmer);

        Assert.Equal("Crop activity not found.", await ApiAssert.MessageAsync(
            await farmer.PutAsJsonAsync("/api/activities/9999", Body(CropActivityType.Irrigation, CrSeed.Today, CrSeed.Irrigation())),
            HttpStatusCode.NotFound));
        Assert.Equal("Crop activity not found.",
            await ApiAssert.MessageAsync(await farmer.DeleteAsync("/api/activities/9999"), HttpStatusCode.NotFound));
    }

    [Fact]
    public async Task API07a_EditLock_AnActivityOlderThanAWeekCannotBeSavedWithItsOwnDate()
    {
        using var factory = new FieldCultivationApiFactory();
        var cycle = await SeedCycleAsync(factory);
        var old = await SeedActivityAsync(factory, cycle, 1, daysAgo: 8);

        using var farmer = factory.CreateClientAs(1, Farmer);
        var response = await farmer.PutAsJsonAsync($"/api/activities/{old.Id}",
            Body(CropActivityType.Irrigation, old.Date, CrSeed.Irrigation(4, 2)));

        Assert.Contains("cannot be older than the past week", await ApiAssert.MessageAsync(response, HttpStatusCode.BadRequest));
    }

    [Fact]
    public async Task API07b_Update_ReRunsTheDetailsValidation()
    {
        using var factory = new FieldCultivationApiFactory();
        var cycle = await SeedCycleAsync(factory);
        var activity = await SeedActivityAsync(factory, cycle, 1);

        using var farmer = factory.CreateClientAs(1, Farmer);
        var response = await farmer.PutAsJsonAsync($"/api/activities/{activity.Id}",
            Body(CropActivityType.Pesticide, CrSeed.Today, CrSeed.Pesticide(quantity: 0)));

        Assert.Equal("Pesticide quantity must be a positive number greater than 0.",
            await ApiAssert.MessageAsync(response, HttpStatusCode.BadRequest));
    }

    // ---------------------------------------------------------------- list all

    [Fact]
    public async Task API08a_AllActivities_AFarmerOnlyEverSeesTheirOwn()
    {
        using var factory = new FieldCultivationApiFactory();
        var mine = await SeedCycleAsync(factory, farmerId: 1);
        var theirs = await SeedCycleAsync(factory, farmerId: 2);
        var own = await SeedActivityAsync(factory, mine, 1);
        await SeedActivityAsync(factory, theirs, 2);

        using var farmer = factory.CreateClientAs(1, Farmer);
        var body = await ApiAssert.JsonAsync(await farmer.GetAsync("/api/activities?farmerId=2"), HttpStatusCode.OK);

        Assert.Equal(own.Id, Assert.Single(body.EnumerateArray()).GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task API08b_AllActivities_OfficersFilterByFarmerCycleAndType()
    {
        using var factory = new FieldCultivationApiFactory();
        var a = await SeedCycleAsync(factory, farmerId: 1);
        var b = await SeedCycleAsync(factory, farmerId: 2);
        await SeedActivityAsync(factory, a, 1, type: CropActivityType.Irrigation);
        var aFert = await SeedActivityAsync(factory, a, 1, type: CropActivityType.Fertilizer, details: CrSeed.Fertilizer());
        var bFert = await SeedActivityAsync(factory, b, 2, type: CropActivityType.Fertilizer, details: CrSeed.Fertilizer());

        using var officer = factory.CreateClientAs(90, Officer);
        async Task<int[]> Ids(string query) =>
            (await ApiAssert.JsonAsync(await officer.GetAsync("/api/activities" + query), HttpStatusCode.OK))
                .EnumerateArray().Select(x => x.GetProperty("id").GetInt32()).OrderBy(x => x).ToArray();

        Assert.Equal(3, (await Ids("")).Length);
        Assert.Equal(2, (await Ids("?farmerId=1")).Length);
        Assert.Equal(new[] { bFert.Id }, await Ids($"?cycleId={b.Id}"));
        Assert.Equal(new[] { aFert.Id, bFert.Id }.OrderBy(x => x), await Ids("?activityType=fertilizer"));
        Assert.Equal(3, (await Ids("?activityType=NotAType")).Length); // an unknown type is ignored, not an error
    }
}
