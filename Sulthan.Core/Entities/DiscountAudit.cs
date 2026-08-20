namespace Sulthan.Core.Entities;

/// <summary>
/// Immutable record of a successful manager-approved discount change.
/// Manager credentials are never stored.
/// </summary>
public sealed class DiscountAudit : BaseEntity
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public decimal SubTotal { get; set; }
    public decimal PreviousDiscount { get; set; }
    public decimal ApprovedDiscount { get; set; }
    public decimal GrandTotal { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int RequestedByUserId { get; set; }
    public User RequestedByUser { get; set; } = null!;
    public int ApprovedByUserId { get; set; }
    public User ApprovedByUser { get; set; } = null!;
}
