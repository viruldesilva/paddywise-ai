using Microsoft.AspNetCore.Mvc;
using Moq;
using PaddyWise.Api.Controllers.ReportingApproval;
using PaddyWise.Api.Services.ReportingApproval;
using Xunit;

namespace PaddyWise.Backend.Tests;

public class ReviewsControllerTests
{
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
