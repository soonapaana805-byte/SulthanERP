namespace Sulthan.Core.DTOs.Orders.Response;

public class OrderItemResponseDto
{
    public int MenuItemId { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public decimal Total => Price * Quantity;

    public string? Notes { get; set; }
}