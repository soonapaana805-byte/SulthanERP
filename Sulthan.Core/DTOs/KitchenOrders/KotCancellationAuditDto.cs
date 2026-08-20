using Sulthan.Core.Enums;

namespace Sulthan.Core.DTOs.KitchenOrders;

public sealed class KotCancellationAuditDto
{
    public int Id { get; set; }
    public int KitchenOrderTicketId { get; set; }
    public int OrderId { get; set; }
    public string KotNumber { get; set; } = string.Empty;
    public string BillNumber { get; set; } = string.Empty;
    public KotCancellationSource Source { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int RequestedByUserId { get; set; }
    public string RequestedByName { get; set; } = string.Empty;
    public int ApprovedByUserId { get; set; }
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
    public List<KotCancellationAuditItemDto> Items { get; set; } = [];
}

public sealed class KotCancellationAuditItemDto
{
    public int MenuItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string KitchenName { get; set; } = string.Empty;
    public decimal CancelledQuantity { get; set; }
}
