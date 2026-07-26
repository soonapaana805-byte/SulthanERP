using Sulthan.Core.DTOs.Billing;

namespace Sulthan.Core.Interfaces;

public interface IBillingService
{
    Task<BillResponseDto?> GetBillAsync(int orderId);

    Task<BillResponseDto?> ReprintBillAsync(string billNumber);

    Task<string?> PrintBillAsync(string billNumber);
}