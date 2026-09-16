using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaddyWise.Api.DTOs.FieldCultivation;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.Shared;
using PaddyWise.Api.Services.FieldCultivation;
using System.Security.Claims;

namespace PaddyWise.Api.Controllers.FieldCultivation;

[ApiController]
[Route("api/cycles")]
public class CyclesController : ControllerBase
{
    private readonly ICycleService _cycleService;

    public CyclesController(ICycleService cycleService)
    {
        _cycleService = cycleService;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetCycles([FromQuery] int? fieldId)
    {
        var callerId = GetCallerId();
        var callerRole = GetCallerRole();
        if (callerId == null || callerRole == null)
            return Unauthorized(new { message = "Invalid access token. Please log in again." });

        try
        {
            var result = await _cycleService.GetCyclesAsync(callerId.Value, callerRole.Value, fieldId);
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
            var result = await _cycleService.GetByIdAsync(id, callerId.Value, callerRole.Value);
            if (result == null)
                return NotFound(new { message = "Cultivation cycle not found." });

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Start Cultivation Cycle — the business operation of this component. Lives here
    /// rather than on FieldsController because everything it creates is a cycle.
    /// </summary>
    [Authorize(Roles = "Farmer")]
    [HttpPost("/api/fields/{fieldId:int}/start-cultivation")]
    public async Task<IActionResult> StartCultivation(int fieldId, CreateCycleRequestDto request)
    {
        var farmerId = GetCallerId();
        if (farmerId == null)
            return Unauthorized(new { message = "Invalid access token. Please log in again." });

        // The route is the authority on which field is being sown.
        request.FieldId = fieldId;

        try
        {
            var result = await _cycleService.StartCycleAsync(farmerId.Value, request);
            if (result == null)
                return NotFound(new { message = "Field not found." });

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

    [Authorize(Roles = "Farmer,AgriculturalOfficer,FieldOfficer")]
    [HttpPost("{id:int}/stages")]
    public async Task<IActionResult> LogStage(int id, LogStageRequestDto request)
    {
        var callerId = GetCallerId();
        var callerRole = GetCallerRole();
        if (callerId == null || callerRole == null)
            return Unauthorized(new { message = "Invalid access token. Please log in again." });

        try
        {
            var result = await _cycleService.LogStageAsync(id, callerId.Value, callerRole.Value, request);
            if (result == null)
                return NotFound(new { message = "Cultivation cycle not found." });

            return Ok(result);
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

    [Authorize(Roles = "Farmer")]
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateCycleStatusRequestDto request)
    {
        var farmerId = GetCallerId();
        if (farmerId == null)
            return Unauthorized(new { message = "Invalid access token. Please log in again." });

        if (!Enum.TryParse<CycleStatus>(request.Status, true, out var status) || !Enum.IsDefined(status))
            return BadRequest(new { message = "Status must be Planned, Active, Harvested or Abandoned." });

        try
        {
            var result = await _cycleService.UpdateStatusAsync(id, farmerId.Value, status);
            if (result == null)
                return NotFound(new { message = "Cultivation cycle not found." });

            return Ok(result);
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
