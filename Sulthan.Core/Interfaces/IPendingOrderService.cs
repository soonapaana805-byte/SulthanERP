using Sulthan.Core.DTOs.Checkout;
using Sulthan.Core.DTOs.PendingOrders;

namespace Sulthan.Core.Interfaces;

public interface IPendingOrderService
{
    Task<PendingOrderDto> CreateAsync(
        CreatePendingOrderDto dto,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PendingOrderDto>> GetPendingAsync(
        CancellationToken cancellationToken = default);

    Task<PendingOrderDto?> GetByIdAsync(
        int orderId,
        CancellationToken cancellationToken = default);

    Task<PendingOrderPrintPreviewDto> GetBillPrintPreviewAsync(
        int orderId,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);

    Task<PendingOrderDto> MarkBillPrintedAsync(
        int orderId,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);

    Task<PendingOrderDto> QueueBillReprintAsync(
        int orderId,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);

    Task<CheckoutResponseDto> CheckoutAsync(
        int orderId,
        PendingOrderCheckoutDto dto,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);

    Task<NextBillNumberPreviewDto> GetNextBillNumberPreviewAsync(
        CancellationToken cancellationToken = default);
}
