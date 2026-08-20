using Sulthan.Core.DTOs.Auth;
using Sulthan.Core.DTOs.Billing;
using Sulthan.Core.DTOs.KitchenOrders;

namespace Sulthan.Core.Interfaces;

public interface IBillingService
{
    Task<BillResponseDto?> GetBillAsync(int orderId);

    Task<BillResponseDto?> ReprintBillAsync(string billNumber);

    Task<string?> PrintBillAsync(string billNumber);

    Task<bool> QueueReceiptReprintAsync(
        string billNumber,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiscountAuditDto>> GetDiscountAuditsAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default);

    Task<BillLifecycleDto?> GetBillLifecycleAsync(
        int orderId,
        CancellationToken cancellationToken = default);

    Task<BillLifecycleDto> CancelBillAsync(
        int orderId,
        ManagerApprovalDto approval,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);

    Task<BillLifecycleDto> VoidBillAsync(
        int orderId,
        ManagerApprovalDto approval,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BillActionAuditDto>> GetBillActionAuditsAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default);

    Task<KotCancellationResultDto> CancelKotAsync(
        int kitchenOrderTicketId,
        ManagerApprovalDto approval,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KotCancellationAuditDto>> GetKotCancellationAuditsAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default);
}
