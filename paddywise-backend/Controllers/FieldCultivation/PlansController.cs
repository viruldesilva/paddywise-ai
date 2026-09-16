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

    public PlansController(ICultivationPlanService planService)
    {
        _planService = planService;
    }

    /// <summary>
    /// Ask the Cultivation Planning Agent for a plan. Lives here rather than on
    /// CyclesController because everything it creates is a plan.
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

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
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
