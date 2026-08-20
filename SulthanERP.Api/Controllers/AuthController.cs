using Microsoft.AspNetCore.Mvc;
using Sulthan.Core.DTOs.Auth;
using Sulthan.Core.Interfaces;

namespace SulthanERP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return Unauthorized(new
            {
                Message = ex.Message
            });
        }
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Cashier")]
    [HttpPost("validate-discount-approval")]
    public async Task<IActionResult> ValidateDiscountApproval(
        ManagerApprovalDto approval,
        CancellationToken cancellationToken)
    {
        try
        {
            await _authService.ValidateActiveAdminAsync(
                approval,
                cancellationToken);

            return Ok(new { approved = true });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new
            {
                message = "Invalid Admin credentials."
            });
        }
    }
}
