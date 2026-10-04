using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PaddyWise.Api.Controllers.Shared;
using PaddyWise.Api.Data;
using PaddyWise.Api.DTOs.Shared;
using PaddyWise.Api.Entities.Shared;
using PaddyWise.Api.Services.Shared;
using Xunit;

namespace PaddyWise.Backend.Tests;

public class AdminControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "AdminTestDb_" + Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    // =========================================================================
    // AUTHORIZATION / RBAC TESTING (WebApplicationFactory)
    // =========================================================================

    [Fact]
    public async Task GetPendingOfficerRequests_AnonymousCaller_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/admin/officer-requests");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Farmer")]
    [InlineData("AgriculturalOfficer")]
    public async Task GetPendingOfficerRequests_NonAdminCaller_Returns403Forbidden(string role)
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = TestJwtHelper.GenerateToken(99, "Test User", "testuser@example.com", role);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/admin/officer-requests");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("Farmer")]
    [InlineData("AgriculturalOfficer")]
    public async Task ApproveOfficerRequest_NonAdminCaller_Returns403Forbidden(string role)
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = TestJwtHelper.GenerateToken(99, "Non Admin", "nonadmin@example.com", role);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsync("/api/admin/officer-requests/10/approve", null);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("Farmer")]
    [InlineData("AgriculturalOfficer")]
    public async Task RejectOfficerRequest_NonAdminCaller_Returns403Forbidden(string role)
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = TestJwtHelper.GenerateToken(99, "Non Admin", "nonadmin@example.com", role);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/officer-requests/10/reject", new RejectOfficerRequestDto { Reason = "Invalid" });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetPendingOfficerRequests_AdminCaller_Returns200Ok()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = TestJwtHelper.GenerateToken(1, "System Admin", "admin@paddywise.lk", "Admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/admin/officer-requests");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // =========================================================================
    // FUNCTIONAL & UNIT TESTING
    // =========================================================================

    [Fact]
    public async Task GetPendingOfficerRequests_ReturnsOnlyPendingOfficers()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockEmail = new Mock<IEmailService>();

        context.Users.AddRange(
            new User { Id = 1, Name = "Pending Officer 1", Email = "po1@test.com", Role = UserRole.AgriculturalOfficer, AccountStatus = AccountStatus.PendingApproval },
            new User { Id = 2, Name = "Pending Officer 2", Email = "po2@test.com", Role = UserRole.AgriculturalOfficer, AccountStatus = AccountStatus.PendingApproval },
            new User { Id = 3, Name = "Approved Officer", Email = "ao@test.com", Role = UserRole.AgriculturalOfficer, AccountStatus = AccountStatus.Approved },
            new User { Id = 4, Name = "Farmer User", Email = "farmer@test.com", Role = UserRole.Farmer, AccountStatus = AccountStatus.Approved }
        );
        await context.SaveChangesAsync();

        var controller = new AdminController(context, mockEmail.Object, NullLogger<AdminController>.Instance);

        // Act
        var actionResult = await controller.GetPendingOfficerRequests();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var requests = Assert.IsAssignableFrom<IEnumerable<OfficerRequestDto>>(okResult.Value);
        Assert.Equal(2, requests.Count());
        Assert.All(requests, r => Assert.Contains("Pending Officer", r.Name));
    }

    [Fact]
    public async Task ApproveOfficerRequest_ValidOfficer_UpdatesStatusToApprovedAndDispatchesEmail()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockEmail = new Mock<IEmailService>();

        var officer = new User
        {
            Id = 10,
            Name = "Officer Samantha",
            Email = "samantha@gov.lk",
            Role = UserRole.AgriculturalOfficer,
            AccountStatus = AccountStatus.PendingApproval
        };
        context.Users.Add(officer);
        await context.SaveChangesAsync();

        var controller = new AdminController(context, mockEmail.Object, NullLogger<AdminController>.Instance);

        // Act
        var actionResult = await controller.ApproveOfficerRequest(10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(200, okResult.StatusCode);

        var updatedUser = await context.Users.FindAsync(10);
        Assert.NotNull(updatedUser);
        Assert.Equal(AccountStatus.Approved, updatedUser.AccountStatus);

        mockEmail.Verify(e => e.SendOfficerApprovalEmailAsync("samantha@gov.lk", "Officer Samantha"), Times.Once);
    }

    [Fact]
    public async Task ApproveOfficerRequest_NonExistentUser_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockEmail = new Mock<IEmailService>();
        var controller = new AdminController(context, mockEmail.Object, NullLogger<AdminController>.Instance);

        // Act
        var actionResult = await controller.ApproveOfficerRequest(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Equal(404, notFoundResult.StatusCode);
        mockEmail.Verify(e => e.SendOfficerApprovalEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ApproveOfficerRequest_UserNotOfficer_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockEmail = new Mock<IEmailService>();

        var farmer = new User
        {
            Id = 20,
            Name = "Farmer Perera",
            Email = "perera@farm.lk",
            Role = UserRole.Farmer,
            AccountStatus = AccountStatus.Approved
        };
        context.Users.Add(farmer);
        await context.SaveChangesAsync();

        var controller = new AdminController(context, mockEmail.Object, NullLogger<AdminController>.Instance);

        // Act
        var actionResult = await controller.ApproveOfficerRequest(20);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Equal(404, notFoundResult.StatusCode);
        mockEmail.Verify(e => e.SendOfficerApprovalEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RejectOfficerRequest_ValidOfficer_UpdatesStatusToRejectedAndDoesNotSendEmail()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockEmail = new Mock<IEmailService>();

        var officer = new User
        {
            Id = 30,
            Name = "Applicant Wickrama",
            Email = "wickrama@agri.lk",
            Role = UserRole.AgriculturalOfficer,
            AccountStatus = AccountStatus.PendingApproval
        };
        context.Users.Add(officer);
        await context.SaveChangesAsync();

        var controller = new AdminController(context, mockEmail.Object, NullLogger<AdminController>.Instance);

        // Act
        var actionResult = await controller.RejectOfficerRequest(30, new RejectOfficerRequestDto { Reason = "Invalid service ID" });

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(200, okResult.StatusCode);

        var updatedUser = await context.Users.FindAsync(30);
        Assert.NotNull(updatedUser);
        Assert.Equal(AccountStatus.Rejected, updatedUser.AccountStatus);

        // Spec requirement: No email sent on rejection
        mockEmail.Verify(e => e.SendOfficerApprovalEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
