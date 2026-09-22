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

        var created = await _activityService.CreateActivityAsync(cycleId, request, userId);
        
        return CreatedAtAction(nameof(GetActivities), new { cycleId = created.CultivationCycleId }, created);
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
}
