using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Backend.Tests.FieldCultivation.Helpers;
using Xunit;

namespace PaddyWise.Backend.Tests.FieldCultivation.Api;

/// <summary>
/// CyclesController end to end: starting a cycle (the component's business operation), its
/// calendar rules, reading, stage logging and status changes.
/// </summary>
[Trait("Component", "FieldCultivation")]
public class CyclesApiTests
{
    private const string Farmer = "Farmer";
    private const string Officer = "AgriculturalOfficer";
    private const string FieldOfficer = "FieldOfficer";
    private const string Admin = "Admin";

    private sealed record Seeded(int FieldId, int VarietyId, int DurationDays);

    /// <summary>A farmer (id 1) with one active field and no cycles yet.</summary>
    private static async Task<Seeded> SeedEmptyFieldAsync(FieldCultivationApiFactory factory, int farmerId = 1)
    {
        await using var db = factory.CreateDbContext();
        var division = await FcTestDb.SeedDivisionAsync(db);
        var variety = await FcTestDb.SeedVarietyAsync(db, durationDays: 105);
        await FcTestDb.SeedUserAsync(db, farmerId, $"Farmer {farmerId}");
        var field = await FcTestDb.SeedFieldAsync(db, farmerId, division);
        return new Seeded(field.Id, variety.Id, variety.DurationDays);
    }

    private static object StartBody(int varietyId, DateOnly sowing, string season = "Maha", int? year = null,
        string method = "Transplanting", string? notes = null) => new
    {
        varietyId,
        season,
        year = year ?? sowing.Year,
        method,
        sowingDate = sowing.ToString("yyyy-MM-dd"),
        notes
    };

    // ---------------------------------------------------------------- start a cycle

    [Theory]
    [InlineData(10, "Planned")]  // sown in the future
    [InlineData(0, "Active")]    // sown today
    public async Task API07a_StartCycle_StatusFollowsTheSowingDate(int daysFromToday, string expectedStatus)
    {
        using var factory = new FieldCultivationApiFactory();
        var seeded = await SeedEmptyFieldAsync(factory);
        var sowing = FcTestDb.Today.AddDays(daysFromToday);

        using var farmer = factory.CreateClientAs(1, Farmer);
        var response = await farmer.PostAsJsonAsync(
            $"/api/fields/{seeded.FieldId}/start-cultivation", StartBody(seeded.VarietyId, sowing));

        var body = await ApiAssert.JsonAsync(response, HttpStatusCode.Created);
        Assert.Equal(expectedStatus, body.GetProperty("status").GetString());
        Assert.Equal("Nursery", body.GetProperty("currentStage").GetString());
        Assert.Equal(
            sowing.AddDays(seeded.DurationDays).ToString("yyyy-MM-dd"),
            body.GetProperty("expectedHarvestDate").GetString());
        Assert.Equal(6, body.GetProperty("timeline").GetArrayLength());
    }

    [Fact]
    public async Task API07b_SeasonAndMethod_AreCaseInsensitive()
    {
        using var factory = new FieldCultivationApiFactory();
        var seeded = await SeedEmptyFieldAsync(factory);

        using var farmer = factory.CreateClientAs(1, Farmer);
        var response = await farmer.PostAsJsonAsync($"/api/fields/{seeded.FieldId}/start-cultivation",
            StartBody(seeded.VarietyId, FcTestDb.Today, season: "maha", method: "directseeding"));

        var body = await ApiAssert.JsonAsync(response, HttpStatusCode.Created);
        Assert.Equal("Maha", body.GetProperty("season").GetString());
        Assert.Equal("DirectSeeding", body.GetProperty("method").GetString());
    }

    [Fact]
    public async Task API08a_AFieldWithAnOpenCycle_CannotStartAnother()
    {
        using var factory = new FieldCultivationApiFactory();
        var seeded = await SeedEmptyFieldAsync(factory);
        using var farmer = factory.CreateClientAs(1, Farmer);
        await farmer.PostAsJsonAsync($"/api/fields/{seeded.FieldId}/start-cultivation", StartBody(seeded.VarietyId, FcTestDb.Today));

        var second = await farmer.PostAsJsonAsync($"/api/fields/{seeded.FieldId}/start-cultivation",
            StartBody(seeded.VarietyId, FcTestDb.Today, season: "Yala"));

        Assert.Equal("Field already has an active cultivation cycle",
            await ApiAssert.MessageAsync(second, HttpStatusCode.BadRequest));
    }

    [Fact]
    public async Task API08b_ADuplicateSeasonAndYear_IsRefusedEvenAfterTheFirstIsClosed()
    {
        using var factory = new FieldCultivationApiFactory();
        var seeded = await SeedEmptyFieldAsync(factory);
        using var farmer = factory.CreateClientAs(1, Farmer);
        var first = await ApiAssert.JsonAsync(
            await farmer.PostAsJsonAsync($"/api/fields/{seeded.FieldId}/start-cultivation", StartBody(seeded.VarietyId, FcTestDb.Today)),
            HttpStatusCode.Created);
        await farmer.PatchAsJsonAsync($"/api/cycles/{first.GetProperty("id").GetInt32()}/status", new { status = "Abandoned" });

        var again = await farmer.PostAsJsonAsync($"/api/fields/{seeded.FieldId}/start-cultivation",
            StartBody(seeded.VarietyId, FcTestDb.Today));

        Assert.Contains("already has a Maha", await ApiAssert.MessageAsync(again, HttpStatusCode.BadRequest));
    }

    [Theory]
    [InlineData("Monsoon", "Transplanting", "Season must be either Yala or Maha.")]
    [InlineData("Maha", "Dibbling", "Method must be Broadcasting, Transplanting or DirectSeeding.")]
    public async Task API08c_ABadSeasonOrMethod_Returns400WithMessage(string season, string method, string message)
    {
        using var factory = new FieldCultivationApiFactory();
        var seeded = await SeedEmptyFieldAsync(factory);
        using var farmer = factory.CreateClientAs(1, Farmer);

        var response = await farmer.PostAsJsonAsync($"/api/fields/{seeded.FieldId}/start-cultivation",
            StartBody(seeded.VarietyId, FcTestDb.Today, season: season, method: method));

        Assert.Equal(message, await ApiAssert.MessageAsync(response, HttpStatusCode.BadRequest));
    }

    [Fact]
    public async Task API08d_AnUnknownVariety_Returns400WithMessage()
    {
        using var factory = new FieldCultivationApiFactory();
        var seeded = await SeedEmptyFieldAsync(factory);
        using var farmer = factory.CreateClientAs(1, Farmer);

        var response = await farmer.PostAsJsonAsync($"/api/fields/{seeded.FieldId}/start-cultivation",
            StartBody(9999, FcTestDb.Today));

        Assert.Equal("Variety not found", await ApiAssert.MessageAsync(response, HttpStatusCode.BadRequest));
    }

    [Theory]
    [InlineData(-30, true)]
    [InlineData(-31, false)]
    [InlineData(365, true)]
    [InlineData(366, false)]
    public async Task API09a_SowingDateWindow_30DaysBackTo365Ahead(int daysFromToday, bool expectedValid)
    {
        using var factory = new FieldCultivationApiFactory();
        var seeded = await SeedEmptyFieldAsync(factory);
        using var farmer = factory.CreateClientAs(1, Farmer);
        var sowing = FcTestDb.Today.AddDays(daysFromToday);

        var response = await farmer.PostAsJsonAsync(
            $"/api/fields/{seeded.FieldId}/start-cultivation", StartBody(seeded.VarietyId, sowing));

        if (expectedValid)
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        else
            Assert.Contains("Sowing date cannot be more than", await ApiAssert.MessageAsync(response, HttpStatusCode.BadRequest));
    }

    [Theory]
    [InlineData(2000, true)]
    [InlineData(2100, true)]
    [InlineData(1999, false)]
    [InlineData(2101, false)]
    public async Task API09b_YearRange_2000To2100(int year, bool expectedValid)
    {
        using var factory = new FieldCultivationApiFactory();
        var seeded = await SeedEmptyFieldAsync(factory);
        using var farmer = factory.CreateClientAs(1, Farmer);

        var response = await farmer.PostAsJsonAsync(
            $"/api/fields/{seeded.FieldId}/start-cultivation", StartBody(seeded.VarietyId, FcTestDb.Today, year: year));

        if (expectedValid)
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        else
            Assert.Contains("Year must be between 2000 and 2100.", await ApiAssert.ModelErrorsAsync(response));
    }

    [Theory]
    [InlineData(1000, true)]
    [InlineData(1001, false)]
    public async Task API09c_NotesLength_1000(int length, bool expectedValid)
    {
        using var factory = new FieldCultivationApiFactory();
        var seeded = await SeedEmptyFieldAsync(factory);
        using var farmer = factory.CreateClientAs(1, Farmer);

        var response = await farmer.PostAsJsonAsync($"/api/fields/{seeded.FieldId}/start-cultivation",
            StartBody(seeded.VarietyId, FcTestDb.Today, notes: new string('n', length)));

        if (expectedValid)
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        else
            Assert.Contains("Notes cannot exceed 1000 characters.", await ApiAssert.ModelErrorsAsync(response));
    }

    [Fact]
    public async Task API10_StartCycle_AsOfficerOrOnAnotherFarmersField_Returns403()
    {
        using var factory = new FieldCultivationApiFactory();
        var seeded = await SeedEmptyFieldAsync(factory, farmerId: 2);

        using var officer = factory.CreateClientAs(99, Officer);
        Assert.Equal(HttpStatusCode.Forbidden, (await officer.PostAsJsonAsync(
            $"/api/fields/{seeded.FieldId}/start-cultivation", StartBody(seeded.VarietyId, FcTestDb.Today))).StatusCode);

        using var farmerA = factory.CreateClientAs(1, Farmer);
        await ApiAssert.MessageAsync(await farmerA.PostAsJsonAsync(
            $"/api/fields/{seeded.FieldId}/start-cultivation", StartBody(seeded.VarietyId, FcTestDb.Today)), HttpStatusCode.Forbidden);
    }

    // ---------------------------------------------------------------- read

    [Fact]
    public async Task API11a_AFarmerSeesOnlyTheirOwnCycles_NewestSowingFirst()
    {
        using var factory = new FieldCultivationApiFactory();
        await using (var db = factory.CreateDbContext())
        {
            var older = await FcTestDb.SeedFarmerCycleAsync(db, 1, "QA Farmer", sownDaysAgo: 200, status: CycleStatus.Harvested);
            var field = await db.Fields.SingleAsync(f => f.Id == older.FieldId);
            var variety = await db.Varieties.FirstAsync();
            await FcTestDb.SeedCycleAsync(db, field, variety, sownDaysAgo: 10, season: Season.Yala);
            await FcTestDb.SeedFarmerCycleAsync(db, 2, "Other Farmer");
        }

        using var farmer = factory.CreateClientAs(1, Farmer);
        var cycles = await ApiAssert.JsonAsync(await farmer.GetAsync("/api/cycles"), HttpStatusCode.OK);

        var dates = cycles.EnumerateArray().Select(c => c.GetProperty("sowingDate").GetString()).ToList();
        Assert.Equal(2, dates.Count);
        Assert.Equal(dates.OrderByDescending(d => d), dates);
        Assert.All(cycles.EnumerateArray(), c => Assert.Equal("QA Farmer's Field", c.GetProperty("fieldName").GetString()));
    }

    [Fact]
    public async Task API11b_AnOfficerMustNameAField_AndFieldIdFilters()
    {
        using var factory = new FieldCultivationApiFactory();
        int fieldB;
        await using (var db = factory.CreateDbContext())
        {
            await FcTestDb.SeedFarmerCycleAsync(db, 1, "QA Farmer");
            fieldB = (await FcTestDb.SeedFarmerCycleAsync(db, 2, "Other Farmer")).FieldId;
        }

        using var officer = factory.CreateClientAs(99, Officer);
        Assert.Equal("fieldId is required.",
            await ApiAssert.MessageAsync(await officer.GetAsync("/api/cycles"), HttpStatusCode.BadRequest));

        var filtered = await ApiAssert.JsonAsync(await officer.GetAsync($"/api/cycles?fieldId={fieldB}"), HttpStatusCode.OK);
        Assert.Equal(fieldB, Assert.Single(filtered.EnumerateArray()).GetProperty("fieldId").GetInt32());

        using var farmerA = factory.CreateClientAs(1, Farmer);
        var none = await ApiAssert.JsonAsync(await farmerA.GetAsync($"/api/cycles?fieldId={fieldB}"), HttpStatusCode.OK);
        Assert.Equal(0, none.GetArrayLength());
    }

    [Fact]
    public async Task API11c_GetById_OtherFarmer403_Officer200_Unknown404()
    {
        using var factory = new FieldCultivationApiFactory();
        int cycleId;
        await using (var db = factory.CreateDbContext())
            cycleId = (await FcTestDb.SeedFarmerCycleAsync(db, 2, "Other Farmer")).Id;

        using var farmerA = factory.CreateClientAs(1, Farmer);
        await ApiAssert.MessageAsync(await farmerA.GetAsync($"/api/cycles/{cycleId}"), HttpStatusCode.Forbidden);

        using var officer = factory.CreateClientAs(99, Officer);
        var body = await ApiAssert.JsonAsync(await officer.GetAsync($"/api/cycles/{cycleId}"), HttpStatusCode.OK);
        Assert.Equal("Tillering", body.GetProperty("expectedStageToday").GetString());

        await ApiAssert.MessageAsync(await officer.GetAsync("/api/cycles/9999"), HttpStatusCode.NotFound);
    }

    // ---------------------------------------------------------------- stage logs

    [Theory]
    [InlineData(1, Farmer, HttpStatusCode.OK)]          // the owner
    [InlineData(99, Officer, HttpStatusCode.OK)]
    [InlineData(98, FieldOfficer, HttpStatusCode.OK)]
    [InlineData(97, Admin, HttpStatusCode.Forbidden)]   // CLAUDE.md: the backend refuses Admin stage logs
    [InlineData(2, Farmer, HttpStatusCode.Forbidden)]   // another farmer
    public async Task API12a_LogStage_WhoMayLogIt(int userId, string role, HttpStatusCode expected)
    {
        using var factory = new FieldCultivationApiFactory();
        int cycleId;
        await using (var db = factory.CreateDbContext())
        {
            cycleId = (await FcTestDb.SeedFarmerCycleAsync(db, 1, "QA Farmer")).Id;
            foreach (var (id, name, r) in new[] { (99, "Officer", PaddyWise.Api.Entities.Shared.UserRole.AgriculturalOfficer),
                         (98, "Field Officer", PaddyWise.Api.Entities.Shared.UserRole.FieldOfficer) })
                await FcTestDb.SeedUserAsync(db, id, name, r);
        }

        using var client = factory.CreateClientAs(userId, role);
        var response = await client.PostAsJsonAsync($"/api/cycles/{cycleId}/stages",
            new { stage = "Tillering", observedOn = FcTestDb.Today.ToString("yyyy-MM-dd"), notes = "Tillers visible." });

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task API12b_LoggingHarvest_ClosesTheCycle()
    {
        using var factory = new FieldCultivationApiFactory();
        int cycleId;
        await using (var db = factory.CreateDbContext())
            cycleId = (await FcTestDb.SeedFarmerCycleAsync(db, 1, "QA Farmer", sownDaysAgo: 118)).Id;

        using var farmer = factory.CreateClientAs(1, Farmer);
        var body = await ApiAssert.JsonAsync(await farmer.PostAsJsonAsync($"/api/cycles/{cycleId}/stages",
            new { stage = "harvest", observedOn = FcTestDb.Today.ToString("yyyy-MM-dd") }), HttpStatusCode.OK);

        Assert.Equal("Harvested", body.GetProperty("status").GetString());
        Assert.Equal("Harvest", body.GetProperty("currentStage").GetString());
        Assert.Equal(FcTestDb.Today.ToString("yyyy-MM-dd"), body.GetProperty("actualHarvestDate").GetString());
        Assert.Equal(1, body.GetProperty("stageLogs").GetArrayLength());
    }

    [Fact]
    public async Task API12c_ABadStage_Returns400WithMessage_AndLongNotesFailModelValidation()
    {
        using var factory = new FieldCultivationApiFactory();
        int cycleId;
        await using (var db = factory.CreateDbContext())
            cycleId = (await FcTestDb.SeedFarmerCycleAsync(db, 1, "QA Farmer")).Id;

        using var farmer = factory.CreateClientAs(1, Farmer);
        var bad = await farmer.PostAsJsonAsync($"/api/cycles/{cycleId}/stages",
            new { stage = "Ripening", observedOn = FcTestDb.Today.ToString("yyyy-MM-dd") });
        Assert.StartsWith("Stage must be one of", await ApiAssert.MessageAsync(bad, HttpStatusCode.BadRequest));

        var longNotes = await farmer.PostAsJsonAsync($"/api/cycles/{cycleId}/stages",
            new { stage = "Tillering", observedOn = FcTestDb.Today.ToString("yyyy-MM-dd"), notes = new string('n', 1001) });
        Assert.Contains("Notes cannot exceed 1000 characters.", await ApiAssert.ModelErrorsAsync(longNotes));
    }

    // ---------------------------------------------------------------- status

    [Fact]
    public async Task API13a_UpdateStatus_BadValue400_Officer403_Abandon200()
    {
        using var factory = new FieldCultivationApiFactory();
        int cycleId;
        await using (var db = factory.CreateDbContext())
            cycleId = (await FcTestDb.SeedFarmerCycleAsync(db, 1, "QA Farmer")).Id;

        using var farmer = factory.CreateClientAs(1, Farmer);
        Assert.Equal("Status must be Planned, Active, Harvested or Abandoned.", await ApiAssert.MessageAsync(
            await farmer.PatchAsJsonAsync($"/api/cycles/{cycleId}/status", new { status = "Paused" }), HttpStatusCode.BadRequest));

        using var officer = factory.CreateClientAs(99, Officer);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await officer.PatchAsJsonAsync($"/api/cycles/{cycleId}/status", new { status = "Abandoned" })).StatusCode);

        var abandoned = await ApiAssert.JsonAsync(
            await farmer.PatchAsJsonAsync($"/api/cycles/{cycleId}/status", new { status = "abandoned" }), HttpStatusCode.OK);
        Assert.Equal("Abandoned", abandoned.GetProperty("status").GetString());
    }

    [Fact]
    public async Task API13b_UpdateStatus_OnAnotherFarmersCycle_Returns403()
    {
        using var factory = new FieldCultivationApiFactory();
        int cycleId;
        await using (var db = factory.CreateDbContext())
            cycleId = (await FcTestDb.SeedFarmerCycleAsync(db, 2, "Other Farmer")).Id;

        using var farmerA = factory.CreateClientAs(1, Farmer);
        await ApiAssert.MessageAsync(
            await farmerA.PatchAsJsonAsync($"/api/cycles/{cycleId}/status", new { status = "Abandoned" }), HttpStatusCode.Forbidden);
    }

    [Fact(Skip = "Known bug (finding 3): CycleService.UpdateStatusAsync accepts any status change, e.g. Harvested→Planned.")]
    public async Task API14_InvalidStatusTransition_IsRejected()
    {
        // A harvested cycle is finished: moving it back to Planned must be refused.
        using var factory = new FieldCultivationApiFactory();
        int cycleId;
        await using (var db = factory.CreateDbContext())
            cycleId = (await FcTestDb.SeedFarmerCycleAsync(db, 1, "QA Farmer", status: CycleStatus.Harvested)).Id;

        using var farmer = factory.CreateClientAs(1, Farmer);
        var response = await farmer.PatchAsJsonAsync($"/api/cycles/{cycleId}/status", new { status = "Planned" });

        await ApiAssert.MessageAsync(response, HttpStatusCode.BadRequest);
    }
}
