using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaddyWise.Api.DTOs.FieldCultivation;
using PaddyWise.Api.Entities.Shared;
using PaddyWise.Api.Services.FieldCultivation;
using System.Security.Claims;

namespace PaddyWise.Api.Controllers.FieldCultivation;

[ApiController]
[Route("api/fields")]
public class FieldsController : ControllerBase
{
    private readonly IFieldService _fieldService;

    public FieldsController(IFieldService fieldService)
    {
        _fieldService = fieldService;
    }

    [Authorize(Roles = "Farmer")]
    [HttpGet]
    public async Task<IActionResult> GetMyFields()
    {
        var farmerId = GetCallerId();
        if (farmerId == null)
            return Unauthorized(new { message = "Invalid access token. Please log in again." });

        var result = await _fieldService.GetMyFieldsAsync(farmerId.Value);
        return Ok(result);
    }

    [Authorize(Roles = "AgriculturalOfficer,FieldOfficer,Admin")]
    [HttpGet("division/{divisionId:int}")]
    public async Task<IActionResult> GetByDivision(int divisionId)
    {
        var result = await _fieldService.GetFieldsByDivisionAsync(divisionId);
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
            var result = await _fieldService.GetByIdAsync(id, callerId.Value, callerRole.Value);
            if (result == null)
                return NotFound(new { message = "Field not found." });

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Farmer")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateFieldRequestDto request)
    {
        var farmerId = GetCallerId();
        if (farmerId == null)
            return Unauthorized(new { message = "Invalid access token. Please log in again." });

        try
        {
            var result = await _fieldService.CreateAsync(farmerId.Value, request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Farmer")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateFieldRequestDto request)
    {
        var farmerId = GetCallerId();
        if (farmerId == null)
            return Unauthorized(new { message = "Invalid access token. Please log in again." });

        try
        {
            var result = await _fieldService.UpdateAsync(id, farmerId.Value, request);
            if (result == null)
                return NotFound(new { message = "Field not found." });

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
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var farmerId = GetCallerId();
        if (farmerId == null)
            return Unauthorized(new { message = "Invalid access token. Please log in again." });

        try
        {
            var deleted = await _fieldService.SoftDeleteAsync(id, farmerId.Value);
            if (!deleted)
                return NotFound(new { message = "Field not found." });

            return NoContent();
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
