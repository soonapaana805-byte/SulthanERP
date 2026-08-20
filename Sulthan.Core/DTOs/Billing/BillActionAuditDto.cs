using Sulthan.Core.Enums;

namespace Sulthan.Core.DTOs.Billing;

public sealed class BillActionAuditDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public BillActionType ActionType { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int RequestedByUserId { get; set; }
    public string RequestedByUserName { get; set; } = string.Empty;
    public int ApprovedByUserId { get; set; }
    public string ApprovedByUserName { get; set; } = string.Empty;
    public DateTime ActionOn { get; set; }
    public string PreviousOrderStatus { get; set; } = string.Empty;
    public string NewOrderStatus { get; set; } = string.Empty;
    public string? PreviousPaymentStatus { get; set; }
    public string? NewPaymentStatus { get; set; }
    public decimal FinancialAmount { get; set; }
    public string? PreviousTableStatus { get; set; }
    public string? NewTableStatus { get; set; }
}
