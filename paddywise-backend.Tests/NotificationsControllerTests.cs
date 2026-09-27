using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaddyWise.Api.Controllers.ReportingApproval;
using PaddyWise.Api.Data;
using PaddyWise.Api.DTOs.ReportingApproval;
using PaddyWise.Api.Entities.ReportingApproval;
using Xunit;

namespace PaddyWise.Backend.Tests;

public class NotificationsControllerTests
{
    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static void SetCaller(ControllerBase controller, int userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, "Farmer")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task GetNotifications_ReturnsOnlyCallerNotifications_OrderedByDateDesc()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var farmerId = 55;
        var otherUserId = 99;

        context.Notifications.AddRange(
            new Notification { Id = 1, UserId = farmerId, Title = "Update 1", Message = "First", CreatedAt = DateTime.UtcNow.AddMinutes(-10), IsRead = false },
            new Notification { Id = 2, UserId = farmerId, Title = "Update 2", Message = "Second", CreatedAt = DateTime.UtcNow.AddMinutes(-2), IsRead = true },
            new Notification { Id = 3, UserId = otherUserId, Title = "Other User", Message = "Not mine", CreatedAt = DateTime.UtcNow, IsRead = false }
        );
        await context.SaveChangesAsync();

        var controller = new NotificationsController(context);
        SetCaller(controller, farmerId);

        // Act
        var result = await controller.GetNotifications(unreadOnly: false);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var notifications = Assert.IsAssignableFrom<List<NotificationResponseDto>>(okResult.Value);

        Assert.Equal(2, notifications.Count);
        Assert.Equal(2, notifications[0].Id); // Most recent first
        Assert.Equal(1, notifications[1].Id);
    }

    [Fact]
    public async Task MarkAsRead_MarksSpecificNotificationAsRead()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var farmerId = 55;

        var notification = new Notification { Id = 10, UserId = farmerId, Title = "Update", Message = "Msg", IsRead = false };
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var controller = new NotificationsController(context);
        SetCaller(controller, farmerId);

        // Act
        var result = await controller.MarkAsRead(10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var updated = await context.Notifications.FindAsync(10);
        Assert.NotNull(updated);
        Assert.True(updated.IsRead);
    }
}
