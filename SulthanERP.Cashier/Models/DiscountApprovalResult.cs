using Sulthan.Core.DTOs.Auth;

namespace SulthanERP.Cashier.Models;

public sealed class DiscountApprovalResult
{
    public decimal DiscountAmount { get; init; }

    public ManagerApprovalDto Approval { get; init; } = new();
}
