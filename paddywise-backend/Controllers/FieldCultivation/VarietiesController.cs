using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaddyWise.Api.Services.FieldCultivation;

namespace PaddyWise.Api.Controllers.FieldCultivation;

[ApiController]
[Route("api/varieties")]
public class VarietiesController : ControllerBase
{
    private readonly ICycleService _cycleService;

    public VarietiesController(ICycleService cycleService)
    {
        _cycleService = cycleService;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _cycleService.GetVarietiesAsync();
        return Ok(result);
    }
}
