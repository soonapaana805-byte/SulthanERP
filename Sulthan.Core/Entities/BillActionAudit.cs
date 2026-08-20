using Sulthan.Core.Enums;

namespace Sulthan.Core.Entities;

/// <summary>
/// Immutable audit record for the single terminal reversal allowed for an order.
/// Approval credentials are validated in memory and are never persisted.
/// </summary>
public sealed class BillActionAudit : BaseEntity
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public string BillNumber { get; set; } = string.Empty;
    public BillActionType ActionType { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int RequestedByUserId { get; set; }
    public User RequestedByUser { get; set; } = null!;
    public int ApprovedByUserId { get; set; }
    public User ApprovedByUser { get; set; } = null!;
    public DateTime ActionOn { get; set; }
    public string PreviousOrderStatus { get; set; } = string.Empty;
    public string NewOrderStatus { get; set; } = string.Empty;
    public string? PreviousPaymentStatus { get; set; }
    public string? NewPaymentStatus { get; set; }
    public decimal FinancialAmount { get; set; }
    public string? PreviousTableStatus { get; set; }
    public string? NewTableStatus { get; set; }
}
