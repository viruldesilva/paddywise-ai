using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaddyWise.Api.DTOs.PestDisease;
using PaddyWise.Api.Services.PestDisease;

namespace PaddyWise.Api.Controllers.PestDisease;

/// <summary>Admin-managed pest/disease reference data. CropAnalysisAgent's
/// get_pest_knowledge tool reads this same table read-only; this controller is the
/// only place it can be written.</summary>
[ApiController]
[Route("api/pest-disease-knowledge")]
public class PestDiseaseKnowledgeController : ControllerBase
{
    private readonly IPestDiseaseKnowledgeService _knowledgeService;

    public PestDiseaseKnowledgeController(IPestDiseaseKnowledgeService knowledgeService)
    {
        _knowledgeService = knowledgeService;
    }

    /// <summary>Every knowledge base entry, alphabetical by name. Any authenticated caller —
    /// officers and farmers benefit from seeing the same reference data the agent uses.</summary>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _knowledgeService.GetAllAsync();
        return Ok(result);
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _knowledgeService.GetByIdAsync(id);
        if (result == null)
            return NotFound(new { message = "Knowledge base entry not found." });

        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(SavePestDiseaseKnowledgeRequestDto request)
    {
        try
        {
            var result = await _knowledgeService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, SavePestDiseaseKnowledgeRequestDto request)
    {
        try
        {
            var result = await _knowledgeService.UpdateAsync(id, request);
            if (result == null)
                return NotFound(new { message = "Knowledge base entry not found." });

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _knowledgeService.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { message = "Knowledge base entry not found." });

        return NoContent();
    }
}
