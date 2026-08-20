using Sulthan.Core.DTOs.CaptainOrders;
using Sulthan.Core.DTOs.PendingOrders;

namespace Sulthan.Core.Interfaces;

public interface ICaptainOrderService
{
    Task<IReadOnlyList<PendingOrderDto>> GetOpenOrdersAsync(
        CancellationToken cancellationToken = default);

    Task<PendingOrderDto?> GetByIdAsync(
        int orderId,
        CancellationToken cancellationToken = default);

    Task<PendingOrderDto?> GetByTableAsync(
        int diningTableId,
        CancellationToken cancellationToken = default);

    Task<PendingOrderDto> CreateAsync(
        CreateCaptainOrderDto dto,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);

    Task<PendingOrderDto> AddItemsAsync(
        int orderId,
        AddCaptainOrderItemsDto dto,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);

    Task<PendingOrderDto> RequestBillAsync(
        int orderId,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);

    Task<PendingOrderDto> QueueRequestedBillPrintAsync(
        int orderId,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);
}
