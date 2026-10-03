using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PaddyWise.Api.Controllers.ReportingApproval;
using PaddyWise.Api.Services.ReportingApproval;
using Xunit;

namespace PaddyWise.Backend.Tests;

public class ReviewsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ReviewsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // =========================================================================
    // AUTHORIZATION / RBAC TESTING (WebApplicationFactory)
    // =========================================================================

    [Fact]
    public async Task GetPlanDraftRevisionComment_AnonymousCaller_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/reviews/plans/42/draft-revision-comment");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPlanDraftRevisionComment_FarmerCaller_Returns403Forbidden()
    {
        // Arrange: Farmer is blocked from accessing officer-only review drafts
        var client = _factory.CreateClient();
        var farmerToken = TestJwtHelper.GenerateToken(10, "Farmer User", "farmer@test.com", "Farmer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", farmerToken);

        // Act
        var response = await client.GetAsync("/api/reviews/plans/42/draft-revision-comment");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetReportDraftRevisionComment_FarmerCaller_Returns403Forbidden()
    {
        // Arrange: Farmer is blocked from accessing officer-only review drafts
        var client = _factory.CreateClient();
        var farmerToken = TestJwtHelper.GenerateToken(10, "Farmer User", "farmer@test.com", "Farmer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", farmerToken);

        // Act
        var response = await client.GetAsync("/api/reviews/reports/99/draft-revision-comment");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("AgriculturalOfficer")]
    [InlineData("Admin")]
    public async Task GetPlanDraftRevisionComment_AuthorizedOfficerOrAdmin_Returns200Ok(string role)
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = TestJwtHelper.GenerateToken(11, "Officer User", "officer@test.com", role);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act (Service returns safe fallback for non-existent id without throwing)
        var response = await client.GetAsync("/api/reviews/plans/9999/draft-revision-comment");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPlanDraftRevisionComment_MalformedId_Returns404NotFoundNot500()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = TestJwtHelper.GenerateToken(11, "Officer User", "officer@test.com", "AgriculturalOfficer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/reviews/plans/invalid-id/draft-revision-comment");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // =========================================================================
    // UNIT TESTS WITH MOQS
    // =========================================================================

    [Fact]
    public async Task GetPlanDraftRevisionComment_ReturnsOkResultWithDraftComment()
    {
        // Arrange
        var mockService = new Mock<IRevisionDraftService>();
        var expectedComment = "Please review section 2 regarding seeding density.";
        mockService
            .Setup(s => s.DraftPlanRevisionCommentAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedComment);

        var controller = new ReviewsController(mockService.Object);

        // Act
        var actionResult = await controller.GetPlanDraftRevisionComment(42, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.NotNull(okResult.Value);

        var property = okResult.Value.GetType().GetProperty("draftComment");
        Assert.NotNull(property);
        var actualComment = property.GetValue(okResult.Value) as string;
        Assert.Equal(expectedComment, actualComment);
    }

    [Fact]
    public async Task GetReportDraftRevisionComment_ReturnsOkResultWithDraftComment()
    {
        // Arrange
        var mockService = new Mock<IRevisionDraftService>();
        var expectedComment = "Please provide additional photos of the leaf stem.";
        mockService
            .Setup(s => s.DraftReportRevisionCommentAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedComment);

        var controller = new ReviewsController(mockService.Object);

        // Act
        var actionResult = await controller.GetReportDraftRevisionComment(99, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.NotNull(okResult.Value);

        var property = okResult.Value.GetType().GetProperty("draftComment");
        Assert.NotNull(property);
        var actualComment = property.GetValue(okResult.Value) as string;
        Assert.Equal(expectedComment, actualComment);
    }
}
