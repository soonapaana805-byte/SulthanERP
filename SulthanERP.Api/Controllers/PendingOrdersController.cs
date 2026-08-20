using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sulthan.Core.DTOs.PendingOrders;
using Sulthan.Core.Interfaces;

namespace SulthanERP.Api.Controllers;

[Authorize(Roles = "Admin,Cashier")]
[ApiController]
[Route("api/[controller]")]
public sealed class PendingOrdersController : ControllerBase
{
    private readonly IPendingOrderService _pendingOrderService;

    public PendingOrdersController(IPendingOrderService pendingOrderService)
    {
        _pendingOrderService = pendingOrderService;
    }

    /// <summary>
    /// Returns a display-only next bill number preview. It does not reserve or increment the bill counter.
    /// </summary>
    [HttpGet("next-bill-number")]
    public async Task<ActionResult<NextBillNumberPreviewDto>> GetNextBillNumberPreview(
        CancellationToken cancellationToken)
    {
        return Ok(await _pendingOrderService.GetNextBillNumberPreviewAsync(cancellationToken));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PendingOrderDto>>> GetPending(
        CancellationToken cancellationToken)
    {
        return Ok(await _pendingOrderService.GetPendingAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PendingOrderDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _pendingOrderService.GetByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<PendingOrderDto>> Create(
        CreatePendingOrderDto dto,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return Unauthorized();

        var result = await _pendingOrderService.CreateAsync(dto, userId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.OrderId }, result);
    }

    [HttpPost("{id:int}/checkout")]
    public async Task<IActionResult> Checkout(
        int id,
        PendingOrderCheckoutDto dto,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return Unauthorized();

        var result = await _pendingOrderService.CheckoutAsync(id, dto, userId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}/bill-print-preview")]
    public async Task<ActionResult<PendingOrderPrintPreviewDto>> GetBillPrintPreview(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return Unauthorized();

        return Ok(await _pendingOrderService.GetBillPrintPreviewAsync(
            id,
            userId,
            cancellationToken));
    }

    [HttpPost("{id:int}/bill-printed")]
    public async Task<ActionResult<PendingOrderDto>> MarkBillPrinted(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return Unauthorized();

        return Ok(await _pendingOrderService.MarkBillPrintedAsync(
            id,
            userId,
            cancellationToken));
    }

    [HttpPost("{id:int}/bill-reprint")]
    public async Task<ActionResult<PendingOrderDto>> ReprintBill(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return Unauthorized();

        return Ok(await _pendingOrderService.QueueBillReprintAsync(
            id,
            userId,
            cancellationToken));
    }

    private bool TryGetAuthenticatedUserId(out int userId)
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
}
