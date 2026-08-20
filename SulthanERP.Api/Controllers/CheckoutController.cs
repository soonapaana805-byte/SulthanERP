using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sulthan.Core.DTOs.Checkout;
using Sulthan.Core.Interfaces;

namespace SulthanERP.Api.Controllers;

[Authorize(Roles = "Admin,Cashier")]
[ApiController]
[Route("api/[controller]")]
public sealed class CheckoutController : ControllerBase
{
    private readonly ICheckoutService _checkoutService;

    public CheckoutController(ICheckoutService checkoutService)
    {
        _checkoutService = checkoutService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateCheckoutDto dto,
        CancellationToken cancellationToken)
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(claimValue, out var userId))
            return Unauthorized();

        var result = await _checkoutService.CheckoutAsync(dto, userId, cancellationToken);
        return Ok(result);
    }
}
