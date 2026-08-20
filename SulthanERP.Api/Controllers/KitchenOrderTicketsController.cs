using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sulthan.Core.DTOs.KitchenOrders;
using Sulthan.Core.Interfaces;
using Sulthan.Core.DTOs.Auth;
using System.Security.Claims;

namespace SulthanERP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class KitchenOrderTicketsController : ControllerBase
{
    private readonly IKitchenOrderTicketService _service;
    private readonly IBillingService _billingService;

    public KitchenOrderTicketsController(
        IKitchenOrderTicketService service,
        IBillingService billingService)
    {
        _service = service;
        _billingService = billingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateKitchenOrderTicketDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Cashier")]
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> CancelSelectedKot(
        int id,
        ManagerApprovalDto approval,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var userId))
        {
            return Unauthorized();
        }

        return Ok(await _billingService.CancelKotAsync(
            id,
            approval,
            userId,
            cancellationToken));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("cancellation-audits")]
    public async Task<IActionResult> GetCancellationAudits(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        return Ok(await _billingService.GetKotCancellationAuditsAsync(
            fromDate,
            toDate,
            cancellationToken));
    }
}
