using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaddyWise.Api.Services.ReportingApproval;

namespace PaddyWise.Api.Controllers.ReportingApproval;

[ApiController]
[Route("api/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IRevisionDraftService _draftService;

    public ReviewsController(IRevisionDraftService draftService)
    {
        _draftService = draftService;
    }

    /// <summary>
    /// Generates an AI-drafted revision comment for a CultivationPlan based on its validation errors.
    /// Read-only suggestion for the officer to review and edit before submitting a decision.
    /// </summary>
    [Authorize(Roles = "AgriculturalOfficer,Admin")]
    [HttpGet("plans/{id:int}/draft-revision-comment")]
    public async Task<IActionResult> GetPlanDraftRevisionComment(int id, CancellationToken ct)
    {
        var draft = await _draftService.DraftPlanRevisionCommentAsync(id, ct);
        return Ok(new { draftComment = draft });
    }

    /// <summary>
    /// Generates an AI-drafted revision comment for a PestDiseaseReport.
    /// Read-only suggestion for the officer to review and edit before submitting a decision.
    /// </summary>
    [Authorize(Roles = "AgriculturalOfficer,Admin")]
    [HttpGet("reports/{id:int}/draft-revision-comment")]
    public async Task<IActionResult> GetReportDraftRevisionComment(int id, CancellationToken ct)
    {
        var draft = await _draftService.DraftReportRevisionCommentAsync(id, ct);
        return Ok(new { draftComment = draft });
    }
}
