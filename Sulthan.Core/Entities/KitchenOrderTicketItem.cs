using Sulthan.Core.Common;

namespace Sulthan.Core.Entities;

public class KitchenOrderTicketItem : BaseEntity
{
    public int KitchenOrderTicketId { get; set; }

    public KitchenOrderTicket? KitchenOrderTicket { get; set; }

    public int MenuItemId { get; set; }

    public MenuItem? MenuItem { get; set; }

    public int? OrderItemId { get; set; }

    public OrderItem? OrderItem { get; set; }

    public decimal Quantity { get; set; }

    public decimal CancelledQuantity { get; set; }

    public string? Notes { get; set; }
}
