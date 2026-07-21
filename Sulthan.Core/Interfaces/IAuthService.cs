using Sulthan.Core.DTOs.Auth;

namespace Sulthan.Core.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
}