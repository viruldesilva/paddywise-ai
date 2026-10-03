using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PaddyWise.Api.DTOs.PestDisease;
using PaddyWise.Api.Entities.PestDisease;
using PaddyWise.Backend.Tests.PestDisease.Helpers;
using Xunit;

namespace PaddyWise.Backend.Tests.PestDisease.Api;

/// <summary>
/// ObservationsController, end to end through a real ASP.NET Core pipeline
/// (WebApplicationFactory) — routing, [Authorize]/[Authorize(Roles=...)], model binding and
/// the controller's own try/catch all run for real. Only the database (InMemory) and
/// authentication (TestAuthHandler, header-driven) are swapped out — see PestDiseaseApiFactory.
/// </summary>
[Trait("Component", "PestDisease")]
public class ObservationApiTests
{
    private const string FarmerRole = "Farmer";
    private const string OfficerRole = "AgriculturalOfficer";
    private const string AdminRole = "Admin";

    [Fact]
    public async Task API01_NoToken_Returns401OnGetObservations()
    {
        using var factory = new PestDiseaseApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/observations");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task API02_FarmerARequestingFarmerBsObservation_Returns403()
    {
        using var factory = new PestDiseaseApiFactory();
        int observationId;
        await using (var context = factory.CreateDbContext())
        {
            var cycleB = await TestDbFactory.SeedFarmerCycleAsync(context, farmerId: 2, farmerName: "Farmer B");
            var observation = await TestDbFactory.SeedObservationAsync(context, cycleB, reportedByUserId: 2);
            observationId = observation.Id;

            // Farmer A also needs to exist as a user for the request to be meaningfully "another farmer".
            await TestDbFactory.SeedFarmerCycleAsync(context, farmerId: 1, farmerName: "Farmer A");
        }

        using var client = factory.CreateClientAs(userId: 1, role: FarmerRole);
        var response = await client.GetAsync($"/api/observations/{observationId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task API03_AgriculturalOfficer_CanReadAnyObservation_Returns200()
    {
        using var factory = new PestDiseaseApiFactory();
        int observationId;
        await using (var context = factory.CreateDbContext())
        {
            var cycleB = await TestDbFactory.SeedFarmerCycleAsync(context, farmerId: 2, farmerName: "Farmer B");
            var observation = await TestDbFactory.SeedObservationAsync(context, cycleB, reportedByUserId: 2);
            observationId = observation.Id;
        }

        using var client = factory.CreateClientAs(userId: 99, role: OfficerRole);
        var response = await client.GetAsync($"/api/observations/{observationId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task API06_UpdatingAnObservationThatAlreadyHasReports_Returns400WithEditLockMessage()
    {
        using var factory = new PestDiseaseApiFactory();
        int observationId;
        await using (var context = factory.CreateDbContext())
        {
            var cycle = await TestDbFactory.SeedFarmerCycleAsync(context, farmerId: 1, farmerName: "Farmer A");
            var observation = await TestDbFactory.SeedObservationAsync(context, cycle, reportedByUserId: 1);
            observationId = observation.Id;

            context.PestDiseaseReports.Add(new PestDiseaseReport
            {
                CropObservationId = observation.Id,
                PossibleIssue = "Rice Blast",
                Confidence = 0.8m,
                Status = PestDiseaseReportStatus.PendingOfficerReview
            });
            await context.SaveChangesAsync();
        }

        using var client = factory.CreateClientAs(userId: 1, role: FarmerRole);
        var response = await client.PutAsJsonAsync($"/api/observations/{observationId}", new UpdateObservationRequestDto
        {
            ObservationType = "Disease",
            Symptoms = "Updated symptoms after the fact.",
            Severity = "Severe"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var message = body.GetProperty("message").GetString();
        Assert.Equal("This observation already has a diagnosis and can no longer be edited.", message);
    }

    [Fact]
    public async Task API07a_EmptySymptoms_Returns400FromAutomaticModelValidation()
    {
        using var factory = new PestDiseaseApiFactory();
        int cultivationCycleId;
        await using (var context = factory.CreateDbContext())
        {
            var cycle = await TestDbFactory.SeedFarmerCycleAsync(context, farmerId: 1, farmerName: "Farmer A");
            cultivationCycleId = cycle.Id;
        }

        using var client = factory.CreateClientAs(userId: 1, role: FarmerRole);
        var response = await client.PostAsJsonAsync("/api/observations", new CreateObservationRequestDto
        {
            CultivationCycleId = cultivationCycleId,
            ObservationType = "Disease",
            Symptoms = string.Empty,
            Severity = "Moderate"
        });

        // Never reaches the controller — ASP.NET's own [Required] model validation on
        // Symptoms short-circuits with its own ValidationProblemDetails shape, not the
        // controller's {message} shape.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Symptoms", body);
    }

    [Fact]
    public async Task API07b_InvalidSeverity_Returns400FromTheServicesOwnValidation()
    {
        using var factory = new PestDiseaseApiFactory();
        int cultivationCycleId;
        await using (var context = factory.CreateDbContext())
        {
            var cycle = await TestDbFactory.SeedFarmerCycleAsync(context, farmerId: 1, farmerName: "Farmer A");
            cultivationCycleId = cycle.Id;
        }

        using var client = factory.CreateClientAs(userId: 1, role: FarmerRole);
        var response = await client.PostAsJsonAsync("/api/observations", new CreateObservationRequestDto
        {
            CultivationCycleId = cultivationCycleId,
            ObservationType = "Disease",
            Symptoms = "Some real symptoms here.",
            Severity = "Extreme" // not a real ObservationSeverity member
        });

        // Passes [Required] model binding (non-empty), reaches the controller, and fails
        // ObservationService.ParseEnum -> InvalidOperationException -> the controller's own
        // {message} shape.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var message = body.GetProperty("message").GetString();
        Assert.Equal("Severity must be Low, Moderate or Severe.", message);
    }

    [Theory]
    [InlineData(2000, HttpStatusCode.Created)]
    [InlineData(2001, HttpStatusCode.BadRequest)]
    public async Task AG15_SymptomsLengthBoundary_2000CharsPass_2001Fail(int length, HttpStatusCode expectedStatus)
    {
        using var factory = new PestDiseaseApiFactory();
        int cultivationCycleId;
        await using (var context = factory.CreateDbContext())
        {
            var cycle = await TestDbFactory.SeedFarmerCycleAsync(context, farmerId: 1, farmerName: "Farmer A");
            cultivationCycleId = cycle.Id;
        }

        using var client = factory.CreateClientAs(userId: 1, role: FarmerRole);
        var response = await client.PostAsJsonAsync("/api/observations", new CreateObservationRequestDto
        {
            CultivationCycleId = cultivationCycleId,
            ObservationType = "Disease",
            Symptoms = new string('a', length),
            Severity = "Moderate"
        });

        Assert.Equal(expectedStatus, response.StatusCode);
    }
}
