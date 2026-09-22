using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaddyWise.Api.DTOs.CropResource;
using PaddyWise.Api.Entities.Shared;
using PaddyWise.Api.Services.CropResource;

namespace PaddyWise.Api.Controllers.CropResource;

[ApiController]
[Route("api/cycles/{cycleId}/activities")]
[Authorize]
public class CropActivitiesController : ControllerBase
{
    private readonly ICropActivityService _activityService;

    public CropActivitiesController(ICropActivityService activityService)
    {
        _activityService = activityService;
    }

    [HttpGet]
    [Authorize(Roles = "Farmer,AgriculturalOfficer,FieldOfficer")]
    public async Task<ActionResult<List<CropActivityDto>>> GetActivities(int cycleId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role)!;

        var activities = await _activityService.GetActivitiesForCycleAsync(cycleId, userId, role);
        return Ok(activities);
    }

    [HttpPost]
    [Authorize(Roles = "Farmer")]
    public async Task<ActionResult<CropActivityDto>> CreateActivity(int cycleId, CreateCropActivityRequestDto request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            var created = await _activityService.CreateActivityAsync(cycleId, request, userId);
            return CreatedAtAction(nameof(GetActivities), new { cycleId = created.CultivationCycleId }, created);
        }
        catch (System.ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (System.InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (System.UnauthorizedAccessException ex)
        {
            return Forbid();
        }
    }

    [HttpGet("/api/activities")]
    [Authorize]
    public async Task<ActionResult<List<CropActivityDto>>> GetAllActivities(
        [FromQuery] int? farmerId,
        [FromQuery] int? cycleId,
        [FromQuery] string? activityType)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role)!;

        if (role == "Farmer")
        {
            farmerId = userId;
        }

        var activities = await _activityService.GetAllActivitiesAsync(farmerId, cycleId, activityType);
        return Ok(activities);
    }

    [HttpPut("/api/activities/{id}")]
    [HttpPut("{id}")]
    [Authorize(Roles = "Farmer,Admin")]
    public async Task<ActionResult<CropActivityDto>> UpdateActivity(int id, UpdateCropActivityRequestDto request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role)!;

        try
        {
            var updated = await _activityService.UpdateActivityAsync(id, request, userId, role);
            return Ok(updated);
        }
        catch (System.ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (System.InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (System.UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("/api/activities/{id}")]
    [HttpDelete("{id}")]
    [Authorize(Roles = "Farmer,Admin")]
    public async Task<IActionResult> DeleteActivity(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role)!;

        try
        {
            await _activityService.DeleteActivityAsync(id, userId, role);
            return NoContent();
        }
        catch (System.InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (System.UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
