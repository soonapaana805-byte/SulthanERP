using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Sulthan.Core.DTOs.Auth;
using Sulthan.Core.Enums;
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

    public async Task<int> ValidateActiveAdminAsync(
        ManagerApprovalDto approval,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(approval.UserName) ||
            string.IsNullOrWhiteSpace(approval.Password))
        {
            throw new UnauthorizedAccessException(
                "Invalid Admin credentials.");
        }

        var reason = approval.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length < 3)
            throw new ArgumentException("Approval reason is required and must contain at least 3 characters.");

        if (reason.Length > 250)
            throw new ArgumentException("Approval reason cannot exceed 250 characters.");

        var userName = approval.UserName.Trim();
        var admin = await _context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.UserName == userName &&
                     x.Role == UserRole.Admin &&
                     x.IsActive,
                cancellationToken);

        if (admin is null ||
            !BCrypt.Net.BCrypt.Verify(approval.Password, admin.PasswordHash))
        {
            throw new UnauthorizedAccessException(
                "Invalid Admin credentials.");
        }

        return admin.Id;
    }
}
