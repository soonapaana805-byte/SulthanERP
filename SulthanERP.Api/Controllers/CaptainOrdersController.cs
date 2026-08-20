using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sulthan.Core.DTOs.CaptainOrders;
using Sulthan.Core.DTOs.PendingOrders;
using Sulthan.Core.Interfaces;

namespace SulthanERP.Api.Controllers;

[Authorize(Roles = "Admin,Captain")]
[ApiController]
[Route("api/[controller]")]
public sealed class CaptainOrdersController : ControllerBase
{
    private readonly ICaptainOrderService _captainOrderService;

    public CaptainOrdersController(ICaptainOrderService captainOrderService)
    {
        _captainOrderService = captainOrderService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PendingOrderDto>>> GetOpenOrders(
        CancellationToken cancellationToken)
    {
        return Ok(await _captainOrderService.GetOpenOrdersAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PendingOrderDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _captainOrderService.GetByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("table/{diningTableId:int}")]
    public async Task<ActionResult<PendingOrderDto>> GetByTable(
        int diningTableId,
        CancellationToken cancellationToken)
    {
        var result = await _captainOrderService.GetByTableAsync(diningTableId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<PendingOrderDto>> Create(
        CreateCaptainOrderDto dto,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return Unauthorized();

        var result = await _captainOrderService.CreateAsync(dto, userId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.OrderId }, result);
    }

    [HttpPost("{id:int}/items")]
    public async Task<ActionResult<PendingOrderDto>> AddItems(
        int id,
        AddCaptainOrderItemsDto dto,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return Unauthorized();

        return Ok(await _captainOrderService.AddItemsAsync(id, dto, userId, cancellationToken));
    }

    [HttpPost("{id:int}/request-bill")]
    public async Task<ActionResult<PendingOrderDto>> RequestBill(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return Unauthorized();

        return Ok(await _captainOrderService.RequestBillAsync(id, userId, cancellationToken));
    }

    [HttpPost("{id:int}/queue-bill-print")]
    public async Task<ActionResult<PendingOrderDto>> QueueBillPrint(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return Unauthorized();

        return Ok(await _captainOrderService.QueueRequestedBillPrintAsync(
            id,
            userId,
            cancellationToken));
    }

    private bool TryGetAuthenticatedUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
