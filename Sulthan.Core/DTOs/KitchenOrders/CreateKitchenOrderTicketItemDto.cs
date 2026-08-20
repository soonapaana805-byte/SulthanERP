using System.ComponentModel.DataAnnotations;

namespace Sulthan.Core.DTOs.KitchenOrders;

public class CreateKitchenOrderTicketItemDto
{
    /// <summary>
    /// Optional for backward compatibility. Selected-KOT cancellation is
    /// unavailable when explicit order-item ownership is not supplied.
    /// </summary>
    public int? OrderItemId { get; set; }

    [Required]
    public int MenuItemId { get; set; }

    public decimal Quantity { get; set; }

    public string? Notes { get; set; }
}
