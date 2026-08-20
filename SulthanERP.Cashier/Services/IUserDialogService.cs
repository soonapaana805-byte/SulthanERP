namespace SulthanERP.Cashier.Services;

using Sulthan.Core.DTOs.Auth;
using Sulthan.Core.Enums;
using SulthanERP.Cashier.Models;

public interface IUserDialogService
{
    void ShowInformation(string message, string title);

    bool PrintReceipt(string receiptText, string jobName);

    DiscountApprovalResult? RequestDiscountApproval(
        decimal subTotal,
        decimal currentDiscount);

    BillActionApprovalResult? RequestBillActionApproval(
        BillActionType actionType,
        string billNumber,
        decimal amount,
        Func<ManagerApprovalDto, Task<string?>> submitAsync,
        string? actionLabel = null);
}
