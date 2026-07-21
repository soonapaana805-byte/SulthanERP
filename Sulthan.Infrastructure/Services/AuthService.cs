using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Sulthan.Core.DTOs.Auth;
using Sulthan.Core.Interfaces;
using Sulthan.Infrastructure.Data;

namespace Sulthan.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly RestaurantDbContext _context;
    private readonly IJwtService _jwtService;

    public AuthService(
        RestaurantDbContext context,
        IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x =>
                x.UserName == request.UserName &&
                x.IsActive);

        if (user == null)
            throw new Exception("Invalid username or password.");

        // Temporary plain text check
        // Later we will replace with BCrypt.Verify()
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new Exception("Invalid username or password.");

        var token = _jwtService.GenerateToken(user);

        return new LoginResponseDto
        {
            Token = token,
            FullName = user.FullName,
            UserName = user.UserName,
            Role = user.Role.ToString()
        };
    }
}