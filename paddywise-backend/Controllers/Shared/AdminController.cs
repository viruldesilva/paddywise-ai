using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaddyWise.Api.Data;
using PaddyWise.Api.DTOs.Shared;
using PaddyWise.Api.Entities.Shared;
using PaddyWise.Api.Services.Shared;

namespace PaddyWise.Api.Controllers.Shared;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ApplicationDbContext context,
        IEmailService emailService,
        ILogger<AdminController> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpGet("officer-requests")]
    public async Task<IActionResult> GetPendingOfficerRequests()
    {
        var requests = await _context.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.AgriculturalOfficer && u.AccountStatus == AccountStatus.PendingApproval)
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new OfficerRequestDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Phone = u.Phone,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(requests);
    }

    [HttpPost("officer-requests/{userId:int}/approve")]
    public async Task<IActionResult> ApproveOfficerRequest(int userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || user.Role != UserRole.AgriculturalOfficer)
        {
            return NotFound(new { message = "Agricultural Officer account not found." });
        }

        user.AccountStatus = AccountStatus.Approved;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Call email service after the status update succeeds
        await _emailService.SendOfficerApprovalEmailAsync(user.Email, user.Name);

        return Ok(new { message = "Officer account approved successfully." });
    }

    [HttpPost("officer-requests/{userId:int}/reject")]
    public async Task<IActionResult> RejectOfficerRequest(int userId, [FromBody] RejectOfficerRequestDto? request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || user.Role != UserRole.AgriculturalOfficer)
        {
            return NotFound(new { message = "Agricultural Officer account not found." });
        }

        user.AccountStatus = AccountStatus.Rejected;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Officer {UserId} ({Email}) application rejected. Reason: {Reason}",
            user.Id, user.Email, request?.Reason ?? "None provided");

        // No email sent on rejection per spec
        return Ok(new { message = "Officer account application rejected." });
    }
}
