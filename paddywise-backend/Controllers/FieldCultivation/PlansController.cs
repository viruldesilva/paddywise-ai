using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaddyWise.Api.DTOs.FieldCultivation;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.Shared;
using PaddyWise.Api.Services.FieldCultivation;
using PaddyWise.Api.Services.ReportingApproval.Agents;
using System.Security.Claims;

namespace PaddyWise.Api.Controllers.FieldCultivation;

[ApiController]
[Route("api/plans")]
public class PlansController : ControllerBase
{
    private readonly ICultivationPlanService _planService;
    private readonly IValidationAgentService _validationAgentService;

    public PlansController(
        ICultivationPlanService planService,
        IValidationAgentService validationAgentService)
    {
        _planService = planService;
        _validationAgentService = validationAgentService;
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

            // Component 4's second pass only checks shape and dosages, and it sets the status
            // either way, so run it only on a plan CultivationPlanValidator passed: it may
            // fail such a plan, but must never promote one this component already failed.
            if (result.Status != nameof(PlanStatus.PendingOfficerApproval))
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);

            await _validationAgentService.ValidateCultivationPlanAsync(result.Id);

            // The second pass can change status, errors and comment, so return the stored plan.
            var validated = await _planService.GetByIdAsync(result.Id, farmerId.Value, UserRole.Farmer) ?? result;

            return CreatedAtAction(nameof(GetById), new { id = validated.Id }, validated);
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
