using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sulthan.Core.DTOs.Auth;
using Sulthan.Core.Interfaces;

using System.Security.Claims;

namespace SulthanERP.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class BillingController : ControllerBase
{
    private readonly IBillingService _billingService;

    public BillingController(IBillingService billingService)
    {
        _billingService = billingService;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("bill-action-audits")]
    public async Task<IActionResult> GetBillActionAudits(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        return Ok(await _billingService.GetBillActionAuditsAsync(
            fromDate,
            toDate,
            cancellationToken));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("discount-audits")]
    public async Task<IActionResult> GetDiscountAudits(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        return Ok(await _billingService.GetDiscountAuditsAsync(
            fromDate,
            toDate,
            cancellationToken));
    }

    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetBill(int orderId)
    {
        var bill = await _billingService.GetBillAsync(orderId);

        if (bill == null)
            return NotFound("Bill not found.");

        return Ok(bill);
    }

    [Authorize(Roles = "Admin,Cashier")]
    [HttpGet("{orderId:int}/lifecycle")]
    public async Task<IActionResult> GetBillLifecycle(
        int orderId,
        CancellationToken cancellationToken)
    {
        var bill = await _billingService.GetBillLifecycleAsync(orderId, cancellationToken);
        return bill is null ? NotFound("Bill not found.") : Ok(bill);
    }

    [Authorize(Roles = "Admin,Cashier")]
    [HttpPost("{orderId:int}/cancel")]
    public async Task<IActionResult> CancelBill(
        int orderId,
        ManagerApprovalDto approval,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return Unauthorized();

        return Ok(await _billingService.CancelBillAsync(
            orderId,
            approval,
            userId,
            cancellationToken));
    }

    [Authorize(Roles = "Admin,Cashier")]
    [HttpPost("{orderId:int}/void")]
    public async Task<IActionResult> VoidBill(
        int orderId,
        ManagerApprovalDto approval,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return Unauthorized();

        return Ok(await _billingService.VoidBillAsync(
            orderId,
            approval,
            userId,
            cancellationToken));
    }

    [HttpGet("reprint/{billNumber}")]
    public async Task<IActionResult> ReprintBill(string billNumber)
    {
        var bill = await _billingService.ReprintBillAsync(billNumber);

        if (bill == null)
            return NotFound("Bill not found.");

        return Ok(bill);
    }
    [HttpGet("print/{billNumber}")]
    public async Task<IActionResult> PrintBill(string billNumber)
    {
        var receipt = await _billingService.PrintBillAsync(billNumber);

        if (receipt == null)
            return NotFound("Bill not found.");

        return Ok(receipt);
    }

    [HttpPost("{billNumber}/reprint")]
    public async Task<IActionResult> QueueReprint(
        string billNumber,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var userId))
        {
            return Unauthorized();
        }

        var queued = await _billingService.QueueReceiptReprintAsync(
            billNumber,
            userId,
            cancellationToken);

        return queued
            ? Accepted(new { message = "Receipt reprint queued." })
            : NotFound("Bill not found.");
    }

    private bool TryGetAuthenticatedUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
