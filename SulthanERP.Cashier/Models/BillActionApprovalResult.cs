using Sulthan.Core.DTOs.Auth;

namespace SulthanERP.Cashier.Models;

public sealed class BillActionApprovalResult
{
    public ManagerApprovalDto Approval { get; init; } = new();
}
