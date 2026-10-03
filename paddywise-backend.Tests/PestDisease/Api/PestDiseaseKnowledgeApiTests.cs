using System.Net;
using System.Net.Http.Json;
using PaddyWise.Api.DTOs.PestDisease;
using PaddyWise.Backend.Tests.PestDisease.Helpers;
using Xunit;

namespace PaddyWise.Backend.Tests.PestDisease.Api;

[Trait("Component", "PestDisease")]
public class PestDiseaseKnowledgeApiTests
{
    private const string FarmerRole = "Farmer";
    private const string AdminRole = "Admin";

    private static SavePestDiseaseKnowledgeRequestDto BuildRequest(string name) => new()
    {
        Name = name,
        Category = "Disease",
        Symptoms = "Diamond-shaped lesions with gray centers on leaves.",
        ManagementGuidance = "Apply recommended fungicide; avoid excess nitrogen.",
        Source = "Sri Lanka Department of Agriculture"
    };

    [Fact]
    public async Task API04a_FarmerCreatingAKnowledgeEntry_Returns403()
    {
        using var factory = new PestDiseaseApiFactory();
        using var client = factory.CreateClientAs(userId: 1, role: FarmerRole);

        var response = await client.PostAsJsonAsync("/api/pest-disease-knowledge", BuildRequest("Rice Blast"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task API04b_AdminCreatingAKnowledgeEntry_Returns201()
    {
        using var factory = new PestDiseaseApiFactory();
        using var client = factory.CreateClientAs(userId: 1, role: AdminRole);

        var response = await client.PostAsJsonAsync("/api/pest-disease-knowledge", BuildRequest("Rice Blast"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task API05_DuplicateNameDifferentCase_Returns400()
    {
        using var factory = new PestDiseaseApiFactory();
        using var client = factory.CreateClientAs(userId: 1, role: AdminRole);

        var first = await client.PostAsJsonAsync("/api/pest-disease-knowledge", BuildRequest("Brown Planthopper"));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var duplicate = await client.PostAsJsonAsync(
            "/api/pest-disease-knowledge", BuildRequest("brown planthopper"));

        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
    }
}
