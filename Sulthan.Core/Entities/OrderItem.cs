namespace Sulthan.Core.Entities;

public class OrderItem : BaseEntity
{
    public int OrderId { get; set; }

    public Order Order { get; set; } = null!;

    public int MenuItemId { get; set; }

    public MenuItem MenuItem { get; set; } = null!;

    public int Quantity { get; set; }

    public int CancelledQuantity { get; set; }

    public decimal Price { get; set; }

    public string? Notes { get; set; }
}
