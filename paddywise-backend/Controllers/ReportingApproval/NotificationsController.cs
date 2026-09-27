using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaddyWise.Api.Data;
using PaddyWise.Api.DTOs.ReportingApproval;

namespace PaddyWise.Api.Controllers.ReportingApproval;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public NotificationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets in-app notifications for the authenticated user, newest first.
    /// Used by mobile and web apps to pop up plain-language updates for farmers.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] bool unreadOnly = false, [FromQuery] int limit = 50)
    {
        var userId = GetCallerId();
        if (userId == null)
            return Unauthorized(new { message = "Invalid access token." });

        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId.Value);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(Math.Clamp(limit, 1, 100))
            .Select(n => new NotificationResponseDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return Ok(notifications);
    }

    /// <summary>
    /// Marks a specific notification as read.
    /// </summary>
    [HttpPatch("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetCallerId();
        if (userId == null)
            return Unauthorized(new { message = "Invalid access token." });

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId.Value);

        if (notification == null)
            return NotFound(new { message = "Notification not found." });

        notification.IsRead = true;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, id = notification.Id, isRead = true });
    }

    /// <summary>
    /// Marks all unread notifications for the caller as read.
    /// </summary>
    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCallerId();
        if (userId == null)
            return Unauthorized(new { message = "Invalid access token." });

        var unread = await _context.Notifications
            .Where(n => n.UserId == userId.Value && !n.IsRead)
            .ToListAsync();

        foreach (var item in unread)
        {
            item.IsRead = true;
        }

        await _context.SaveChangesAsync();

        return Ok(new { success = true, markedCount = unread.Count });
    }

    private int? GetCallerId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var id) ? id : null;
    }
}
