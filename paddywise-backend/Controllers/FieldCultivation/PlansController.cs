using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaddyWise.Api.DTOs.FieldCultivation;
using PaddyWise.Api.Entities.Shared;
using PaddyWise.Api.Services.FieldCultivation;
using System.Security.Claims;

namespace PaddyWise.Api.Controllers.FieldCultivation;

[ApiController]
[Route("api/plans")]
public class PlansController : ControllerBase
{
    private readonly ICultivationPlanService _planService;
    private readonly IPlanGenerationQueue _planQueue;

    public PlansController(
        ICultivationPlanService planService,
        IPlanGenerationQueue planQueue)
    {
        _planService = planService;
        _planQueue = planQueue;
    }

    /// <summary>
    /// Ask the Cultivation Planning Agent for a plan. Lives here rather than on
    /// CyclesController because everything it creates is a plan. Generation takes minutes,
    /// so this saves a Draft, queues it, and answers 202 at once; the client polls
    /// GET /api/plans/{id} until the status is no longer Draft.
    /// </summary>
    [Authorize(Roles = "Farmer")]
    [HttpPost("/api/cycles/{cycleId:int}/plans")]
    public async Task<IActionResult> RequestPlan(int cycleId, RequestPlanDto request)
    {
        var farmerId = GetCallerId();
        if (farmerId == null)
            return Unauthorized(new { message = "Invalid access token. Please log in again." });

        try
        {
            var result = await _planService.RequestPlanAsync(farmerId.Value, cycleId, request.Objective);
            if (result == null)
                return NotFound(new { message = "Cultivation cycle not found." });

            await _planQueue.EnqueueAsync(result.Id);

            return AcceptedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
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
            var result = await _planService.GetByIdAsync(id, callerId.Value, callerRole.Value);
            if (result == null)
                return NotFound(new { message = "Cultivation plan not found." });

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("/api/cycles/{cycleId:int}/plans")]
    public async Task<IActionResult> GetForCycle(int cycleId)
    {
        var callerId = GetCallerId();
        var callerRole = GetCallerRole();
        if (callerId == null || callerRole == null)
            return Unauthorized(new { message = "Invalid access token. Please log in again." });

        try
        {
            var result = await _planService.GetForCycleAsync(cycleId, callerId.Value, callerRole.Value);
            if (result == null)
                return NotFound(new { message = "Cultivation cycle not found." });

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>The officer's approval queue, newest first. ?divisionId= narrows it to one division.</summary>
    [Authorize(Roles = "AgriculturalOfficer")]
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending([FromQuery] int? divisionId)
    {
        var result = await _planService.GetPendingAsync(divisionId);
        return Ok(result);
    }

    /// <summary>Approve, reject or send back a plan that is waiting for approval.</summary>
    [Authorize(Roles = "AgriculturalOfficer")]
    [HttpPost("{id:int}/review")]
    public async Task<IActionResult> Review(int id, ReviewPlanDto request)
    {
        var officerId = GetCallerId();
        if (officerId == null)
            return Unauthorized(new { message = "Invalid access token. Please log in again." });

        try
        {
            var result = await _planService.ReviewAsync(id, officerId.Value, request);
            if (result == null)
                return NotFound(new { message = "Cultivation plan not found." });

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
