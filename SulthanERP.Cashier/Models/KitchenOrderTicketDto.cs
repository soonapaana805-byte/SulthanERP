namespace SulthanERP.Cashier.Models;

/// <summary>
/// Read model for the existing KitchenOrderTickets API response.
/// </summary>
public sealed class KitchenOrderTicketDto
{
    public int Id { get; set; }

    public string KotNumber { get; set; } = string.Empty;

    public int OrderId { get; set; }

    public DateTime CreatedOn { get; set; }

    public string Status { get; set; } = "Active";

    public DateTime? CancelledOn { get; set; }

    public List<KitchenOrderTicketItemDto> Items { get; set; } = [];

    public KitchenOrderSummaryDto? Order { get; set; }
}

public sealed class KitchenOrderTicketItemDto
{
    public int MenuItemId { get; set; }
    public int? OrderItemId { get; set; }
    public decimal Quantity { get; set; }
    public decimal CancelledQuantity { get; set; }
    public KitchenMenuItemDto? MenuItem { get; set; }
    public KitchenOwnedOrderItemDto? OrderItem { get; set; }
}

public sealed class KitchenMenuItemDto
{
    public string Name { get; set; } = string.Empty;
}

public sealed class KitchenOwnedOrderItemDto
{
    public decimal Price { get; set; }
}

public sealed class KitchenOrderSummaryDto
{
    public int Id { get; set; }

    public string BillNumber { get; set; } = string.Empty;

    public int OrderType { get; set; }

    public int BillStatus { get; set; }

    public decimal GrandTotal { get; set; }
}
