using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaddyWise.Api.Data;
using PaddyWise.Api.DTOs.Shared;
using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Api.Controllers.Shared;

[Authorize]
[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public NotificationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all notifications for the current authenticated user.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> GetNotifications([FromQuery] bool? unreadOnly, [FromQuery] string? type)
    {
        var callerId = GetCallerId();
        if (callerId == null)
            return Unauthorized(new { message = "Invalid access token." });

        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == callerId.Value);

        if (unreadOnly == true)
        {
            query = query.Where(n => !n.IsRead);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(n => n.Type == type);
        }

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                Status = n.Status,
                RelatedCycleId = n.RelatedCycleId,
                RelatedRecommendationId = n.RelatedRecommendationId,
                OfficerName = n.OfficerName,
                OfficerComment = n.OfficerComment,
                ActionText = n.ActionText,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return Ok(items);
    }

    /// <summary>
    /// Returns the number of unread notifications for the caller.
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<ActionResult<object>> GetUnreadCount()
    {
        var callerId = GetCallerId();
        if (callerId == null)
            return Unauthorized(new { message = "Invalid access token." });

        var count = await _context.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == callerId.Value && !n.IsRead);

        return Ok(new { unreadCount = count });
    }

    /// <summary>
    /// Marks a specific notification as read.
    /// </summary>
    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var callerId = GetCallerId();
        if (callerId == null)
            return Unauthorized(new { message = "Invalid access token." });

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == callerId.Value);

        if (notification == null)
            return NotFound(new { message = $"Notification #{id} not found." });

        notification.IsRead = true;
        await _context.SaveChangesAsync();

        return Ok(new NotificationDto
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            Status = notification.Status,
            RelatedCycleId = notification.RelatedCycleId,
            RelatedRecommendationId = notification.RelatedRecommendationId,
            OfficerName = notification.OfficerName,
            OfficerComment = notification.OfficerComment,
            ActionText = notification.ActionText,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt
        });
    }

    /// <summary>
    /// Marks all unread notifications as read for the caller.
    /// </summary>
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var callerId = GetCallerId();
        if (callerId == null)
            return Unauthorized(new { message = "Invalid access token." });

        var unread = await _context.Notifications
            .Where(n => n.UserId == callerId.Value && !n.IsRead)
            .ToListAsync();

        foreach (var item in unread)
        {
            item.IsRead = true;
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "All notifications marked as read.", count = unread.Count });
    }

    private int? GetCallerId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var id) ? id : null;
    }
}
