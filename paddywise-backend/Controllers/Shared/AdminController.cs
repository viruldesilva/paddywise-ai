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

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var totalUsers = await _context.Users.CountAsync(ct);
        var activeUsers = await _context.Users.CountAsync(u => u.AccountStatus == AccountStatus.Approved, ct);
        var inactiveUsers = await _context.Users.CountAsync(u => u.AccountStatus != AccountStatus.Approved, ct);
        var pendingOfficerApprovals = await _context.Users.CountAsync(
            u => u.Role == UserRole.AgriculturalOfficer && u.AccountStatus == AccountStatus.PendingApproval, ct);

        var roleCounts = await _context.Users
            .GroupBy(u => u.Role)
            .Select(g => new { Role = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Role.ToString(), x => x.Count, ct);

        var dto = new AdminDashboardDto
        {
            TotalUsers = totalUsers,
            UsersPerRole = new Dictionary<string, int>
            {
                ["Farmer"] = roleCounts.GetValueOrDefault("Farmer", 0),
                ["AgriculturalOfficer"] = roleCounts.GetValueOrDefault("AgriculturalOfficer", 0),
                ["FieldOfficer"] = roleCounts.GetValueOrDefault("FieldOfficer", 0),
                ["Admin"] = roleCounts.GetValueOrDefault("Admin", 0),
            },
            ActiveUsers = activeUsers,
            InactiveUsers = inactiveUsers,
            PendingOfficerApprovals = pendingOfficerApprovals
        };

        return Ok(dto);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var query = _context.Users
            .AsNoTracking()
            .Include(u => u.Division)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(role) && !role.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            if (Enum.TryParse<UserRole>(role, true, out var parsedRole))
            {
                query = query.Where(u => u.Role == parsedRole);
            }
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(u =>
                u.Name.ToLower().Contains(s) ||
                u.Email.ToLower().Contains(s) ||
                (u.Division != null && (u.Division.Name.ToLower().Contains(s) || u.Division.District.ToLower().Contains(s))));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserDto
            {
                Id = u.Id,
                FullName = u.Name,
                Email = u.Email,
                Role = u.Role.ToString(),
                AssignedLocation = u.Role == UserRole.Admin
                    ? "System Console"
                    : (u.Division != null ? u.Division.Name : "Unassigned"),
                Status = u.AccountStatus == AccountStatus.Approved ? "Active" : "Inactive",
                CreatedAt = u.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new AdminUsersPagedResponseDto
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        });
    }

    [HttpDelete("users/{userId:int}")]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        var callerIdValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(callerIdValue, out var callerId) && callerId == userId)
        {
            return BadRequest(new { message = "You cannot delete your own admin account." });
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        try
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return Ok(new { message = "User deleted successfully." });
        }
        catch (DbUpdateException)
        {
            // If historical foreign key constraints prevent permanent removal, deactivate the account.
            _context.Entry(user).State = EntityState.Unchanged;
            user.AccountStatus = AccountStatus.Rejected;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { message = "User has associated records and was deactivated instead of deleted." });
        }
    }
}
