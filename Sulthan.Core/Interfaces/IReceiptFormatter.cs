using Sulthan.Core.DTOs.Billing;

namespace Sulthan.Core.Interfaces;

public interface IReceiptFormatter
{
    Task<string> Generate80mmReceiptAsync(PrintBillDto bill);

    Task<string> Generate58mmReceiptAsync(PrintBillDto bill);
}