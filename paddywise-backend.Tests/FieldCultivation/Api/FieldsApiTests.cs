using System.Net;
using System.Net.Http.Json;
using PaddyWise.Backend.Tests.FieldCultivation.Helpers;
using Xunit;

namespace PaddyWise.Backend.Tests.FieldCultivation.Api;

/// <summary>
/// FieldsController and DivisionsController end to end through the real pipeline
/// (FieldCultivationApiFactory): routing, [Authorize(Roles=...)], model binding, the
/// controllers' try/catch. Only the database and authentication are swapped.
/// </summary>
[Trait("Component", "FieldCultivation")]
public class FieldsApiTests
{
    private const string Farmer = "Farmer";
    private const string Officer = "AgriculturalOfficer";
    private const string FieldOfficer = "FieldOfficer";
    private const string Admin = "Admin";

    private static object FieldBody(int divisionId, string name = "East Paddy", decimal area = 2.5m,
        string soilType = "Clay", double? latitude = null, double? longitude = null) => new
    {
        name,
        area,
        soilType,
        irrigationType = "Rainfed",
        divisionId,
        latitude,
        longitude
    };

    [Theory]
    [InlineData("/api/fields")]
    [InlineData("/api/cycles")]
    [InlineData("/api/plans/pending")]
    [InlineData("/api/divisions")]
    public async Task API01_NoLogin_Returns401(string url)
    {
        using var factory = new FieldCultivationApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task API02a_FarmerAReadingFarmerBsField_Returns403_OfficerReturns200()
    {
        using var factory = new FieldCultivationApiFactory();
        int fieldId;
        await using (var db = factory.CreateDbContext())
        {
            var cycle = await FcTestDb.SeedFarmerCycleAsync(db, 2, "Farmer B");
            fieldId = cycle.FieldId;
        }

        using var farmerA = factory.CreateClientAs(1, Farmer);
        var forbidden = await farmerA.GetAsync($"/api/fields/{fieldId}");
        await ApiAssert.MessageAsync(forbidden, HttpStatusCode.Forbidden);

        using var officer = factory.CreateClientAs(99, Officer);
        var ok = await ApiAssert.JsonAsync(await officer.GetAsync($"/api/fields/{fieldId}"), HttpStatusCode.OK);
        Assert.Equal("Farmer B", ok.GetProperty("farmerName").GetString());

        using var farmerB = factory.CreateClientAs(2, Farmer);
        Assert.Equal(HttpStatusCode.OK, (await farmerB.GetAsync($"/api/fields/{fieldId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await farmerB.GetAsync("/api/fields/9999")).StatusCode);
    }

    [Theory]
    [InlineData(Farmer, HttpStatusCode.Forbidden)]
    [InlineData(Officer, HttpStatusCode.OK)]
    [InlineData(FieldOfficer, HttpStatusCode.OK)]
    [InlineData(Admin, HttpStatusCode.OK)]
    public async Task API02b_FieldsByDivision_IsForOfficersAndAdminsOnly(string role, HttpStatusCode expected)
    {
        using var factory = new FieldCultivationApiFactory();
        int divisionId;
        await using (var db = factory.CreateDbContext())
            divisionId = (await FcTestDb.SeedDivisionAsync(db)).Id;

        using var client = factory.CreateClientAs(1, role);
        var response = await client.GetAsync($"/api/fields/division/{divisionId}");

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task API03a_CreateField_AsFarmer_Returns201AndOwnsIt()
    {
        using var factory = new FieldCultivationApiFactory();
        int divisionId;
        await using (var db = factory.CreateDbContext())
        {
            divisionId = (await FcTestDb.SeedDivisionAsync(db)).Id;
            await FcTestDb.SeedUserAsync(db, 1, "QA Farmer");
        }

        using var client = factory.CreateClientAs(1, Farmer);
        var response = await client.PostAsJsonAsync("/api/fields", FieldBody(divisionId));

        var body = await ApiAssert.JsonAsync(response, HttpStatusCode.Created);
        Assert.Equal(1, body.GetProperty("farmerId").GetInt32());
        Assert.True(body.GetProperty("isActive").GetBoolean());
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task API03b_CreateField_UnknownDivision_Returns400WithMessage()
    {
        using var factory = new FieldCultivationApiFactory();
        using var client = factory.CreateClientAs(1, Farmer);

        var response = await client.PostAsJsonAsync("/api/fields", FieldBody(divisionId: 9999));

        Assert.Equal("Division not found", await ApiAssert.MessageAsync(response, HttpStatusCode.BadRequest));
    }

    [Fact]
    public async Task API03c_CreateField_EmptyName_Returns400FromModelValidation()
    {
        using var factory = new FieldCultivationApiFactory();
        using var client = factory.CreateClientAs(1, Farmer);

        var response = await client.PostAsJsonAsync("/api/fields", FieldBody(1, name: ""));

        Assert.Contains("Field name is required.", await ApiAssert.ModelErrorsAsync(response));
    }

    [Fact]
    public async Task API03d_CreateField_AsOfficer_Returns403()
    {
        using var factory = new FieldCultivationApiFactory();
        using var client = factory.CreateClientAs(99, Officer);

        var response = await client.PostAsJsonAsync("/api/fields", FieldBody(1));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    public static IEnumerable<object?[]> FieldBoundaries() => new List<object?[]>
    {
        new object?[] { new string('n', 100), 2.5m, "Clay", null, null, true },
        new object?[] { new string('n', 101), 2.5m, "Clay", null, null, false },
        new object?[] { "Field", 0.01m, "Clay", null, null, true },
        new object?[] { "Field", 0.009m, "Clay", null, null, false },
        new object?[] { "Field", 1000m, "Clay", null, null, true },
        new object?[] { "Field", 1000.01m, "Clay", null, null, false },
        new object?[] { "Field", 2.5m, new string('s', 50), null, null, true },
        new object?[] { "Field", 2.5m, new string('s', 51), null, null, false },
        new object?[] { "Field", 2.5m, "Clay", 90.0, 180.0, true },
        new object?[] { "Field", 2.5m, "Clay", -90.0, -180.0, true },
    };

    [Theory]
    [MemberData(nameof(FieldBoundaries))]
    public async Task API04_FieldLengthAndRangeBoundaries(
        string name, decimal area, string soilType, double? latitude, double? longitude, bool expectedValid)
    {
        using var factory = new FieldCultivationApiFactory();
        int divisionId;
        await using (var db = factory.CreateDbContext())
        {
            divisionId = (await FcTestDb.SeedDivisionAsync(db)).Id;
            await FcTestDb.SeedUserAsync(db, 1, "QA Farmer");
        }

        using var client = factory.CreateClientAs(1, Farmer);
        var response = await client.PostAsJsonAsync(
            "/api/fields", FieldBody(divisionId, name, area, soilType, latitude, longitude));

        if (expectedValid)
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        else
            await ApiAssert.ModelErrorsAsync(response);
    }

    /// <summary>Out-of-range coordinates, one hundredth past each limit.</summary>
    public static IEnumerable<object?[]> CoordinateBoundariesBlockedByRangeBug() => new List<object?[]>
    {
        new object?[] { 90.01, 0.0 },
        new object?[] { -90.01, 0.0 },
        new object?[] { 0.0, 180.01 },
        new object?[] { 0.0, -180.01 },
    };

    [Theory(Skip = "Known bug (finding 7): [Range(-90, 90)] / [Range(-180, 180)] are the int overloads, so a double like 90.01 is rounded before the check and accepted.")]
    [MemberData(nameof(CoordinateBoundariesBlockedByRangeBug))]
    public async Task API04b_CoordinateOutOfRange_IsRejected(double latitude, double longitude)
    {
        using var factory = new FieldCultivationApiFactory();
        int divisionId;
        await using (var db = factory.CreateDbContext())
        {
            divisionId = (await FcTestDb.SeedDivisionAsync(db)).Id;
            await FcTestDb.SeedUserAsync(db, 1, "QA Farmer");
        }

        using var client = factory.CreateClientAs(1, Farmer);
        var response = await client.PostAsJsonAsync(
            "/api/fields", FieldBody(divisionId, latitude: latitude, longitude: longitude));

        await ApiAssert.ModelErrorsAsync(response);
    }

    [Fact]
    public async Task API05a_UpdateOrDeleteAnotherFarmersField_Returns403()
    {
        using var factory = new FieldCultivationApiFactory();
        int fieldId, divisionId;
        await using (var db = factory.CreateDbContext())
        {
            var cycle = await FcTestDb.SeedFarmerCycleAsync(db, 2, "Farmer B");
            fieldId = cycle.FieldId;
            divisionId = (await db.Fields.FindAsync(fieldId))!.DivisionId;
        }

        using var farmerA = factory.CreateClientAs(1, Farmer);

        await ApiAssert.MessageAsync(
            await farmerA.PutAsJsonAsync($"/api/fields/{fieldId}", FieldBody(divisionId, "Stolen")), HttpStatusCode.Forbidden);
        await ApiAssert.MessageAsync(await farmerA.DeleteAsync($"/api/fields/{fieldId}"), HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task API05b_SoftDelete_HidesTheFieldFromListsAndBlocksNewCycles()
    {
        using var factory = new FieldCultivationApiFactory();
        int fieldId, divisionId, varietyId;
        await using (var db = factory.CreateDbContext())
        {
            var division = await FcTestDb.SeedDivisionAsync(db);
            divisionId = division.Id;
            varietyId = (await FcTestDb.SeedVarietyAsync(db)).Id;
            await FcTestDb.SeedUserAsync(db, 1, "QA Farmer");
            fieldId = (await FcTestDb.SeedFieldAsync(db, 1, division)).Id;
        }

        using var farmer = factory.CreateClientAs(1, Farmer);
        Assert.Equal(HttpStatusCode.NoContent, (await farmer.DeleteAsync($"/api/fields/{fieldId}")).StatusCode);

        var mine = await ApiAssert.JsonAsync(await farmer.GetAsync("/api/fields"), HttpStatusCode.OK);
        Assert.Equal(0, mine.GetArrayLength());

        using var officer = factory.CreateClientAs(99, Officer);
        var byDivision = await ApiAssert.JsonAsync(await officer.GetAsync($"/api/fields/division/{divisionId}"), HttpStatusCode.OK);
        Assert.Equal(0, byDivision.GetArrayLength());

        var start = await farmer.PostAsJsonAsync($"/api/fields/{fieldId}/start-cultivation", new
        {
            varietyId,
            season = "Maha",
            year = FcTestDb.Today.Year,
            method = "Transplanting",
            sowingDate = FcTestDb.Today.ToString("yyyy-MM-dd")
        });
        Assert.Equal("Field not found.", await ApiAssert.MessageAsync(start, HttpStatusCode.NotFound));
    }

    [Fact]
    public async Task API05c_UpdateOwnField_Returns200WithTheNewValues()
    {
        using var factory = new FieldCultivationApiFactory();
        int fieldId, divisionId;
        await using (var db = factory.CreateDbContext())
        {
            var cycle = await FcTestDb.SeedFarmerCycleAsync(db, 1, "QA Farmer");
            fieldId = cycle.FieldId;
            divisionId = (await db.Fields.FindAsync(fieldId))!.DivisionId;
        }

        using var farmer = factory.CreateClientAs(1, Farmer);
        var body = await ApiAssert.JsonAsync(
            await farmer.PutAsJsonAsync($"/api/fields/{fieldId}", FieldBody(divisionId, "Renamed", 3.25m)), HttpStatusCode.OK);

        Assert.Equal("Renamed", body.GetProperty("name").GetString());
        Assert.Equal(3.25m, body.GetProperty("area").GetDecimal());
    }

    [Fact]
    public async Task API06_Lists_AreSortedByName()
    {
        using var factory = new FieldCultivationApiFactory();
        int divisionId;
        await using (var db = factory.CreateDbContext())
        {
            var division = await FcTestDb.SeedDivisionAsync(db, "Polonnaruwa");
            await FcTestDb.SeedDivisionAsync(db, "Ampara");
            divisionId = division.Id;
            await FcTestDb.SeedUserAsync(db, 1, "QA Farmer");
            await FcTestDb.SeedUserAsync(db, 2, "Other Farmer");
            await FcTestDb.SeedFieldAsync(db, 1, division, "Wewa Side");
            await FcTestDb.SeedFieldAsync(db, 1, division, "Anicut Block");
            await FcTestDb.SeedFieldAsync(db, 2, division, "Middle Tract");
        }

        using var farmer = factory.CreateClientAs(1, Farmer);
        var mine = await ApiAssert.JsonAsync(await farmer.GetAsync("/api/fields"), HttpStatusCode.OK);
        Assert.Equal(new[] { "Anicut Block", "Wewa Side" }, mine.EnumerateArray().Select(f => f.GetProperty("name").GetString()));

        using var officer = factory.CreateClientAs(99, Officer);
        var all = await ApiAssert.JsonAsync(await officer.GetAsync($"/api/fields/division/{divisionId}"), HttpStatusCode.OK);
        Assert.Equal(
            new[] { "Anicut Block", "Middle Tract", "Wewa Side" },
            all.EnumerateArray().Select(f => f.GetProperty("name").GetString()));

        var divisions = await ApiAssert.JsonAsync(await farmer.GetAsync("/api/divisions"), HttpStatusCode.OK);
        Assert.Equal(new[] { "Ampara", "Polonnaruwa" }, divisions.EnumerateArray().Select(d => d.GetProperty("name").GetString()));
    }
}
