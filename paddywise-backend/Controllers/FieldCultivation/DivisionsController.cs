using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaddyWise.Api.Services.FieldCultivation;

namespace PaddyWise.Api.Controllers.FieldCultivation;

[ApiController]
[Route("api/divisions")]
public class DivisionsController : ControllerBase
{
    private readonly IFieldService _fieldService;

    public DivisionsController(IFieldService fieldService)
    {
        _fieldService = fieldService;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _fieldService.GetDivisionsAsync();
        return Ok(result);
    }
}
