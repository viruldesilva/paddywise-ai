using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PaddyWise.Api.DTOs.Shared;
using PaddyWise.Api.Services.Shared;

namespace PaddyWise.Api.Controllers.Shared;

[Authorize]
[ApiController]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var callerId = GetCallerId();
        if (callerId == null)
            return Unauthorized(new { message = "Invalid access token." });

        var profile = await _profileService.GetProfileAsync(callerId.Value);
        if (profile == null)
            return NotFound(new { message = "User profile not found." });

        return Ok(profile);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request)
    {
        var callerId = GetCallerId();
        if (callerId == null)
            return Unauthorized(new { message = "Invalid access token." });

        try
        {
            var updated = await _profileService.UpdateProfileAsync(callerId.Value, request);
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        var callerId = GetCallerId();
        if (callerId == null)
            return Unauthorized(new { message = "Invalid access token." });

        try
        {
            await _profileService.ChangePasswordAsync(callerId.Value, request);
            return Ok(new { message = "Password changed successfully." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private int? GetCallerId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var id) ? id : null;
    }
}
