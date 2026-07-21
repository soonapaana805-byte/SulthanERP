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
}