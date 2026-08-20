using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sulthan.Core.DTOs.CashClosings;
using Sulthan.Core.Interfaces;

namespace SulthanERP.Api.Controllers;

[Authorize(Roles = "Admin,Cashier")]
[ApiController]
[Route("api/[controller]")]
public sealed class CashClosingsController : ControllerBase
{
    private readonly ICashClosingService _cashClosingService;

    public CashClosingsController(ICashClosingService cashClosingService)
    {
        _cashClosingService = cashClosingService;
    }

    /// <summary>
    /// Shows the signed-in cashier's current-day collection and closing status.
    /// </summary>
    [HttpGet("today")]
    public async Task<ActionResult<CashClosingSummaryDto>> GetToday(CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return Unauthorized();

        return Ok(await _cashClosingService.GetTodayAsync(userId, cancellationToken));
    }

    /// <summary>
    /// Records today's physical cash count. Expected collection amounts are calculated on the server.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CashClosingSummaryDto>> Create(
        CreateCashClosingDto dto,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return Unauthorized();

        return Ok(await _cashClosingService.CreateTodayAsync(dto, userId, cancellationToken));
    }

    private bool TryGetAuthenticatedUserId(out int userId)
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
}
