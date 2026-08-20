using Sulthan.Core.Enums;

namespace Sulthan.Core.Entities;

/// <summary>Immutable record of one KOT cancellation.</summary>
public sealed class KotCancellationAudit : BaseEntity
{
    public int KitchenOrderTicketId { get; set; }
    public KitchenOrderTicket KitchenOrderTicket { get; set; } = null!;
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public string KotNumber { get; set; } = string.Empty;
    public string BillNumber { get; set; } = string.Empty;
    public KotCancellationSource Source { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int RequestedByUserId { get; set; }
    public User RequestedByUser { get; set; } = null!;
    public string RequestedByName { get; set; } = string.Empty;
    public int ApprovedByUserId { get; set; }
    public User ApprovedByUser { get; set; } = null!;
    public string ApprovedByName { get; set; } = string.Empty;
    public DateTime CancelledOn { get; set; }
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public decimal PreviousSubTotal { get; set; }
    public decimal PreviousDiscount { get; set; }
    public decimal PreviousTax { get; set; }
    public decimal PreviousGrandTotal { get; set; }
    public decimal NewSubTotal { get; set; }
    public decimal NewDiscount { get; set; }
    public decimal NewTax { get; set; }
    public decimal NewGrandTotal { get; set; }
    public ICollection<KotCancellationAuditItem> Items { get; set; } = [];
}
