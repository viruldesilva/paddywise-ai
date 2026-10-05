using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaddyWise.Api.Agents.CropResource;

namespace PaddyWise.Api.Controllers.CropResource;

[ApiController]
[Authorize]
public class CropActivityAnalysisController : ControllerBase
{
    private readonly ICropActivityAnalysisService _analysisService;

    public CropActivityAnalysisController(ICropActivityAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    [HttpPost("api/cycles/{cycleId:int}/analysis")]
    public async Task<ActionResult<CropActivityAnalysisOutput>> RunAnalysis(
        int cycleId,
        [FromBody] ActivityAnalysisInput? input)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        input ??= new ActivityAnalysisInput();
        input.CultivationCycleId = cycleId;

        try
        {
            var result = await _analysisService.AnalyzeActivitiesAsync(input, userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("api/cycles/{cycleId:int}/analysis/latest")]
    public async Task<ActionResult<CropActivityAnalysisOutput>> GetLatestAnalysis(int cycleId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var input = new ActivityAnalysisInput
        {
            CultivationCycleId = cycleId,
            FocusArea = "All"
        };

        try
        {
            var result = await _analysisService.AnalyzeActivitiesAsync(input, userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("api/cycles/{cycleId:int}/ai-chat")]
    public async Task<ActionResult<AiChatResponseDto>> ChatWithAi(
        int cycleId,
        [FromBody] AiChatRequestDto request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        request.CultivationCycleId = cycleId;

        try
        {
            var response = await _analysisService.ChatAboutActivitiesAsync(request, userId);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("api/cycles/{cycleId:int}/recommendations/review")]
    public async Task<ActionResult<ReviewRecommendationResponseDto>> ReviewRecommendation(
        int cycleId,
        [FromBody] ReviewRecommendationRequestDto request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Farmer";

        try
        {
            var result = await _analysisService.ReviewRecommendationAsync(cycleId, request, userId, userRole);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("api/cycles/{cycleId:int}/recommendations")]
    public async Task<ActionResult<List<CropActivityRecommendationDto>>> GetCycleRecommendations(int cycleId)
    {
        var result = await _analysisService.GetCycleRecommendationsAsync(cycleId);
        return Ok(result);
    }

    [HttpGet("api/cycles/{cycleId:int}/agent-audit")]
    public async Task<ActionResult<List<AgentAuditLogEntryDto>>> GetAgentAuditHistory(int cycleId)
    {
        try
        {
            var logs = await _analysisService.GetAuditLogsAsync(cycleId);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // ============================================================
    // AGRICULTURAL OFFICER APPROVAL QUEUE ENDPOINTS
    // ============================================================

    /// <summary>
    /// Returns all crop activity recommendations awaiting Agricultural Officer approval.
    /// Can be filtered by agrarian division.
    /// </summary>
    [HttpGet("api/recommendations/pending")]
    public async Task<ActionResult<List<CropActivityRecommendationDto>>> GetPendingOfficerRecommendations(
        [FromQuery] int? divisionId,
        [FromQuery] string? status = null)
    {
        var result = await _analysisService.GetPendingOfficerRecommendationsAsync(divisionId, status);
        return Ok(result);
    }

    /// <summary>
    /// Agricultural Officer approves or rejects a recommendation with comments.
    /// Once approved, the recommendation displays as verified on the farmer's portal.
    /// </summary>
    [HttpPost("api/recommendations/{id:int}/officer-review")]
    public async Task<ActionResult<CropActivityRecommendationDto>> OfficerReviewRecommendation(
        int id,
        [FromBody] OfficerRecommendationReviewDto request)
    {
        var officerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role);

        if (role != "AgriculturalOfficer" && role != "FieldOfficer" && role != "Admin")
        {
            return Forbid();
        }

        try
        {
            var result = await _analysisService.OfficerReviewRecommendationAsync(id, request.Decision, request.Comment, officerId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
