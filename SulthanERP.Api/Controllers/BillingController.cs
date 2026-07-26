using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sulthan.Core.Interfaces;

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

    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetBill(int orderId)
    {
        var bill = await _billingService.GetBillAsync(orderId);

        if (bill == null)
            return NotFound("Bill not found.");

        return Ok(bill);
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
}