using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PaddyWise.Api.Controllers.Shared;
using PaddyWise.Api.Data;
using PaddyWise.Api.DTOs.Shared;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.Shared;
using PaddyWise.Api.Services.Shared;
using Xunit;

namespace PaddyWise.Backend.Tests;

[Trait("Component", "ReportingApproval")]
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

    [Fact]
    public async Task GetDashboard_AnonymousCaller_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/admin/dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Farmer")]
    [InlineData("AgriculturalOfficer")]
    public async Task GetDashboard_NonAdminCaller_Returns403Forbidden(string role)
    {
        var client = _factory.CreateClient();
        var token = TestJwtHelper.GenerateToken(99, "Non Admin", "nonadmin@example.com", role);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/admin/dashboard");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_AnonymousCaller_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/admin/users");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Farmer")]
    [InlineData("AgriculturalOfficer")]
    public async Task GetUsers_NonAdminCaller_Returns403Forbidden(string role)
    {
        var client = _factory.CreateClient();
        var token = TestJwtHelper.GenerateToken(99, "Non Admin", "nonadmin@example.com", role);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/admin/users");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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

    [Fact]
    public async Task GetDashboard_ReturnsRealAggregatedStats()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockEmail = new Mock<IEmailService>();

        context.Users.AddRange(
            new User { Id = 101, Name = "Farmer 1", Email = "f1@paddy.lk", Role = UserRole.Farmer, AccountStatus = AccountStatus.Approved },
            new User { Id = 102, Name = "Farmer 2", Email = "f2@paddy.lk", Role = UserRole.Farmer, AccountStatus = AccountStatus.Rejected },
            new User { Id = 103, Name = "Officer 1", Email = "o1@paddy.lk", Role = UserRole.AgriculturalOfficer, AccountStatus = AccountStatus.PendingApproval },
            new User { Id = 104, Name = "Admin 1", Email = "a1@paddy.lk", Role = UserRole.Admin, AccountStatus = AccountStatus.Approved }
        );
        await context.SaveChangesAsync();

        var controller = new AdminController(context, mockEmail.Object, NullLogger<AdminController>.Instance);

        // Act
        var result = await controller.GetDashboard(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<AdminDashboardDto>(okResult.Value);

        Assert.Equal(4, dto.TotalUsers);
        Assert.Equal(2, dto.ActiveUsers);
        Assert.Equal(2, dto.InactiveUsers);
        Assert.Equal(1, dto.PendingOfficerApprovals);
        Assert.Equal(2, dto.UsersPerRole["Farmer"]);
        Assert.Equal(1, dto.UsersPerRole["AgriculturalOfficer"]);
        Assert.Equal(1, dto.UsersPerRole["Admin"]);
    }

    [Fact]
    public async Task GetUsers_PaginationAndSearch_ReturnsFilteredResults()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockEmail = new Mock<IEmailService>();

        var divPolonnaruwa = new Division { Id = 1, Name = "Polonnaruwa Central", District = "Polonnaruwa", Province = "North Central" };
        var divAmpara = new Division { Id = 2, Name = "Ampara Central", District = "Ampara", Province = "Eastern" };
        context.Divisions.AddRange(divPolonnaruwa, divAmpara);

        context.Users.AddRange(
            new User { Id = 201, Name = "Sunil Bandara", Email = "sunil@farm.lk", Role = UserRole.Farmer, AccountStatus = AccountStatus.Approved, Division = divPolonnaruwa },
            new User { Id = 202, Name = "Kamal Perera", Email = "kamal@agri.lk", Role = UserRole.AgriculturalOfficer, AccountStatus = AccountStatus.Approved, Division = divAmpara },
            new User { Id = 203, Name = "Sunil Shantha", Email = "sshantha@farm.lk", Role = UserRole.Farmer, AccountStatus = AccountStatus.Approved, Division = divAmpara }
        );
        await context.SaveChangesAsync();

        var controller = new AdminController(context, mockEmail.Object, NullLogger<AdminController>.Instance);

        // Act - search "Sunil"
        var result = await controller.GetUsers(search: "Sunil", role: "Farmer", page: 1, pageSize: 10, ct: CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<AdminUsersPagedResponseDto>(okResult.Value);

        Assert.Equal(2, paged.TotalCount);
        Assert.Equal(2, paged.Items.Count);
        Assert.All(paged.Items, item => Assert.Contains("Sunil", item.FullName));
    }

    [Fact]
    public async Task DeleteUser_PreventSelfDeletion_ReturnsBadRequest()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockEmail = new Mock<IEmailService>();

        var adminUser = new User
        {
            Id = 301,
            Name = "Active Admin",
            Email = "admin@paddywise.lk",
            Role = UserRole.Admin,
            AccountStatus = AccountStatus.Approved
        };
        context.Users.Add(adminUser);
        await context.SaveChangesAsync();

        var controller = new AdminController(context, mockEmail.Object, NullLogger<AdminController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "301"),
                    new Claim(ClaimTypes.Role, "Admin")
                }))
            }
        };

        // Act - Admin attempts to delete their own account
        var result = await controller.DeleteUser(301);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_OtherUser_RemovesUserSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockEmail = new Mock<IEmailService>();

        var userToDelete = new User
        {
            Id = 401,
            Name = "Removable User",
            Email = "removable@paddywise.lk",
            Role = UserRole.Farmer,
            AccountStatus = AccountStatus.Approved
        };
        context.Users.Add(userToDelete);
        await context.SaveChangesAsync();

        var controller = new AdminController(context, mockEmail.Object, NullLogger<AdminController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "999"),
                    new Claim(ClaimTypes.Role, "Admin")
                }))
            }
        };

        // Act
        var result = await controller.DeleteUser(401);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        var exists = await context.Users.AnyAsync(u => u.Id == 401);
        Assert.False(exists);
    }
}
