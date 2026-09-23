using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaddyWise.Api.Agents.CropResource;

namespace PaddyWise.Api.Controllers.CropResource;

[ApiController]
[Route("api/cycles/{cycleId}")]
[Authorize]
public class CropActivityAnalysisController : ControllerBase
{
    private readonly ICropActivityAnalysisService _analysisService;

    public CropActivityAnalysisController(ICropActivityAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    [HttpPost("analysis")]
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
        catch (System.InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (System.UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("analysis/latest")]
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
        catch (System.InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (System.UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("ai-chat")]
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
