using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaddyWise.Api.DTOs.PestDisease;
using PaddyWise.Api.Entities.Shared;
using PaddyWise.Api.Services.PestDisease;
using PaddyWise.Api.Services.ReportingApproval;
using System.Security.Claims;

namespace PaddyWise.Api.Controllers.PestDisease;

[ApiController]
[Route("api/pest-disease-reports")]
public class PestDiseaseReportsController : ControllerBase
{
    private readonly IPestDiseaseReportService _reportService;
    private readonly INotificationMessageService _notificationService;

    public PestDiseaseReportsController(
        IPestDiseaseReportService reportService,
        INotificationMessageService notificationService)
    {
        _reportService = reportService;
        _notificationService = notificationService;
    }

    /// <summary>The officer review queue, or — for a farmer — only diagnoses on their own
    /// observations. ?status=, ?observationId= and ?cultivationCycleId= narrow the list.</summary>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetReports(
        [FromQuery] string? status, [FromQuery] int? observationId, [FromQuery] int? cultivationCycleId)
    {
        var callerId = GetCallerId();
        var callerRole = GetCallerRole();
        if (callerId == null || callerRole == null)
            return Unauthorized(new { message = "Invalid access token. Please log in again." });

        try
        {
            var result = await _reportService.GetReportsAsync(
                callerId.Value, callerRole.Value, status, observationId, cultivationCycleId);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var callerId = GetCallerId();
        var callerRole = GetCallerRole();
        if (callerId == null || callerRole == null)
            return Unauthorized(new { message = "Invalid access token. Please log in again." });

        try
        {
            var result = await _reportService.GetByIdAsync(id, callerId.Value, callerRole.Value);
            if (result == null)
                return NotFound(new { message = "Report not found." });

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>Approve, reject or send back a diagnosis that is waiting for officer review.</summary>
    [Authorize(Roles = "AgriculturalOfficer")]
    [HttpPost("{id:int}/review")]
    public async Task<IActionResult> Review(int id, ReviewPestDiseaseReportDto request)
    {
        var officerId = GetCallerId();
        if (officerId == null)
            return Unauthorized(new { message = "Invalid access token. Please log in again." });

        try
        {
            var result = await _reportService.ReviewAsync(id, officerId.Value, request);
            if (result == null)
                return NotFound(new { message = "Report not found." });

            await _notificationService.GenerateAndCreatePestDiseaseReportNotificationAsync(id);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private int? GetCallerId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var id) ? id : null;
    }

    private UserRole? GetCallerRole()
    {
        var value = User.FindFirst(ClaimTypes.Role)?.Value;
        return Enum.TryParse<UserRole>(value, true, out var role) ? role : null;
    }
}
