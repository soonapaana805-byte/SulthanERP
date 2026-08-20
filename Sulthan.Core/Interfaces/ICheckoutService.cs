using Sulthan.Core.DTOs.Checkout;

namespace Sulthan.Core.Interfaces;

public interface ICheckoutService
{
    Task<CheckoutResponseDto> CheckoutAsync(
        CreateCheckoutDto dto,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);
}
