using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaddyWise.Api.Controllers.Shared;
using PaddyWise.Api.Data;
using PaddyWise.Api.DTOs.Shared;
using PaddyWise.Api.Entities.Shared;
using Xunit;

namespace PaddyWise.Backend.Tests;

public class NotificationsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public NotificationsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "NotifControllerTestDb_" + Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static NotificationsController CreateControllerWithCaller(ApplicationDbContext context, int callerId)
    {
        var controller = new NotificationsController(context);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, callerId.ToString()),
            new Claim(ClaimTypes.Role, "Farmer")
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        return controller;
    }

    // =========================================================================
    // AUTHORIZATION / RBAC TESTING (WebApplicationFactory)
    // =========================================================================

    [Fact]
    public async Task GetNotifications_AnonymousCaller_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/notifications");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUnreadCount_AnonymousCaller_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/notifications/unread-count");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MarkAsRead_AnonymousCaller_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PutAsync("/api/notifications/1/read", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // =========================================================================
    // DATA ISOLATION & INTEGRITY TESTING
    // =========================================================================

    [Fact]
    public async Task GetNotifications_ReturnsOnlyNotificationsBelongingToCaller()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var callerId = 100;
        var otherUserId = 200;

        context.Notifications.AddRange(
            new Notification { Id = 1, UserId = callerId, Title = "Caller Notification 1", Message = "Msg 1", IsRead = false },
            new Notification { Id = 2, UserId = callerId, Title = "Caller Notification 2", Message = "Msg 2", IsRead = true },
            new Notification { Id = 3, UserId = otherUserId, Title = "Other User Notification", Message = "Msg 3", IsRead = false }
        );
        await context.SaveChangesAsync();

        var controller = CreateControllerWithCaller(context, callerId);

        // Act
        var actionResult = await controller.GetNotifications(unreadOnly: false, type: null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var items = Assert.IsAssignableFrom<List<NotificationDto>>(okResult.Value);
        Assert.Equal(2, items.Count);
        Assert.All(items, item => Assert.Equal(callerId, item.UserId));
    }

    [Fact]
    public async Task GetUnreadCount_CalculatesOnlyUnreadForCaller()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var callerId = 101;

        context.Notifications.AddRange(
            new Notification { Id = 10, UserId = callerId, Title = "Unread 1", Message = "M1", IsRead = false },
            new Notification { Id = 11, UserId = callerId, Title = "Unread 2", Message = "M2", IsRead = false },
            new Notification { Id = 12, UserId = callerId, Title = "Read 1", Message = "M3", IsRead = true },
            new Notification { Id = 13, UserId = 999, Title = "Other Unread", Message = "M4", IsRead = false }
        );
        await context.SaveChangesAsync();

        var controller = CreateControllerWithCaller(context, callerId);

        // Act
        var actionResult = await controller.GetUnreadCount();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var unreadProp = okResult.Value?.GetType().GetProperty("unreadCount");
        Assert.NotNull(unreadProp);
        var count = (int)unreadProp.GetValue(okResult.Value)!;
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task MarkAsRead_OtherUsersNotification_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var callerId = 102;
        var otherUserId = 999;

        var notification = new Notification
        {
            Id = 50,
            UserId = otherUserId,
            Title = "Private Notification",
            Message = "Sensitive content",
            IsRead = false
        };
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var controller = CreateControllerWithCaller(context, callerId);

        // Act
        var actionResult = await controller.MarkAsRead(50);

        // Assert: Access is denied by returning 404 (isolation prevents knowing it exists)
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Equal(404, notFoundResult.StatusCode);

        // Confirm it wasn't modified
        var dbItem = await context.Notifications.FindAsync(50);
        Assert.NotNull(dbItem);
        Assert.False(dbItem.IsRead);
    }

    [Fact]
    public async Task MarkAllAsRead_MarksOnlyCallersNotificationsAsRead()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var callerId = 103;

        context.Notifications.AddRange(
            new Notification { Id = 61, UserId = callerId, Title = "Caller 1", Message = "M1", IsRead = false },
            new Notification { Id = 62, UserId = callerId, Title = "Caller 2", Message = "M2", IsRead = false },
            new Notification { Id = 63, UserId = 888, Title = "Other User", Message = "M3", IsRead = false }
        );
        await context.SaveChangesAsync();

        var controller = CreateControllerWithCaller(context, callerId);

        // Act
        var actionResult = await controller.MarkAllAsRead();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(200, okResult.StatusCode);

        var callerItems = await context.Notifications.Where(n => n.UserId == callerId).ToListAsync();
        Assert.All(callerItems, n => Assert.True(n.IsRead));

        var otherItem = await context.Notifications.FindAsync(63);
        Assert.NotNull(otherItem);
        Assert.False(otherItem.IsRead); // untouched
    }
}
