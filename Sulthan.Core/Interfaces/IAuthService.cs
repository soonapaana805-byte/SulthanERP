using Sulthan.Core.DTOs.Auth;

namespace Sulthan.Core.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);

    Task<int> ValidateActiveAdminAsync(
        ManagerApprovalDto approval,
        CancellationToken cancellationToken = default);
}
