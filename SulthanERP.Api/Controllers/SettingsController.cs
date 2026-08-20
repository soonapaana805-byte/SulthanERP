using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sulthan.Core.Entities;
using Sulthan.Core.Interfaces;

namespace SulthanERP.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;

    public SettingsController(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var settings = await _settingsService.GetAsync();

        if (settings == null)
            return NotFound("Settings not found.");

        return Ok(settings);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] Settings settings)
    {
        var result = await _settingsService.UpdateAsync(settings);
        return Ok(result);
    }
}