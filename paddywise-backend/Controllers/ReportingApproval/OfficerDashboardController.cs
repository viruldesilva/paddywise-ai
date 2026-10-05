using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaddyWise.Api.Services.ReportingApproval;

namespace PaddyWise.Api.Controllers.ReportingApproval;

[ApiController]
[Route("api/officer/dashboard")]
public class OfficerDashboardController : ControllerBase
{
    private readonly IOfficerDashboardService _dashboardService;

    public OfficerDashboardController(IOfficerDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Returns the consolidated dashboard data for the authenticated Agricultural Officer,
    /// scoped to the officer's assigned division.
    /// </summary>
    [Authorize(Roles = "AgriculturalOfficer")]
    [HttpGet]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var callerIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(callerIdValue, out var officerId))
        {
            return Unauthorized(new { message = "Invalid access token. Please log in again." });
        }

        try
        {
            var result = await _dashboardService.GetOfficerDashboardAsync(officerId, ct);
            if (result == null)
            {
                return NotFound(new { message = "Agricultural Officer profile not found." });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
