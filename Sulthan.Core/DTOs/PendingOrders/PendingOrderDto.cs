using Sulthan.Core.Enums;

namespace Sulthan.Core.DTOs.PendingOrders;

/// <summary>
/// A pending, unpaid order that has already been sent to the kitchen.
/// </summary>
public sealed class PendingOrderDto
{
    public int OrderId { get; set; }

    public string BillNumber { get; set; } = string.Empty;

    public OrderType OrderType { get; set; }

    public int? DiningTableId { get; set; }

    public string? TableNumber { get; set; }

    public string? TableStatus { get; set; }

    public int? CustomerId { get; set; }

    public string? CustomerName { get; set; }

    public int CreatedByUserId { get; set; }

    public string? CaptainName { get; set; }

    public DateTime CreatedOn { get; set; }

    public decimal SubTotal { get; set; }

    public decimal Discount { get; set; }

    public decimal Tax { get; set; }

    public decimal GrandTotal { get; set; }

    public string? Remarks { get; set; }

    public string KitchenTicketNumber { get; set; } = string.Empty;

    public DateTime? BillRequestedOn { get; set; }

    public DateTime? BillPrintedOn { get; set; }

    public DateTime? PaymentReminderDueOn =>
        BillPrintedOn?.AddMinutes(2);

    public bool IsPaymentReminderDue =>
        PaymentReminderDueOn.HasValue &&
        DateTime.UtcNow >= PaymentReminderDueOn.Value;

    public List<PendingOrderItemDto> Items { get; set; } = [];
}

public sealed class PendingOrderItemDto
{
    public int MenuItemId { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public decimal Total => Price * Quantity;

    public string? Notes { get; set; }
}
