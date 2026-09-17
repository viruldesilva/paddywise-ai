using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaddyWise.Api.DTOs.PestDisease;
using PaddyWise.Api.Entities.Shared;
using PaddyWise.Api.Services.PestDisease;
using System.Security.Claims;

namespace PaddyWise.Api.Controllers.PestDisease;

[ApiController]
[Route("api/observations")]
public class ObservationsController : ControllerBase
{
    private readonly IObservationService _observationService;

    public ObservationsController(IObservationService observationService)
    {
        _observationService = observationService;
    }

    /// <summary>A farmer's own reports, or every report for officers/admins.
    /// ?cultivationCycleId= and ?fieldId= narrow the list.</summary>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetObservations([FromQuery] int? cultivationCycleId, [FromQuery] int? fieldId)
    {
        var callerId = GetCallerId();
        var callerRole = GetCallerRole();
        if (callerId == null || callerRole == null)
            return Unauthorized(new { message = "Invalid access token. Please log in again." });

        var result = await _observationService.GetObservationsAsync(
            callerId.Value, callerRole.Value, cultivationCycleId, fieldId);

        return Ok(result);
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
            var result = await _observationService.GetByIdAsync(id, callerId.Value, callerRole.Value);
            if (result == null)
                return NotFound(new { message = "Observation not found." });

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Farmer")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateObservationRequestDto request)
    {
        var farmerId = GetCallerId();
        if (farmerId == null)
            return Unauthorized(new { message = "Invalid access token. Please log in again." });

        try
        {
            var result = await _observationService.CreateAsync(farmerId.Value, request);
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

    [Authorize(Roles = "Farmer")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateObservationRequestDto request)
    {
        var farmerId = GetCallerId();
        if (farmerId == null)
            return Unauthorized(new { message = "Invalid access token. Please log in again." });

        try
        {
            var result = await _observationService.UpdateAsync(id, farmerId.Value, request);
            if (result == null)
                return NotFound(new { message = "Observation not found." });

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
