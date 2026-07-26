using System.ComponentModel.DataAnnotations;

namespace Sulthan.Core.DTOs.KitchenOrders;

public class CreateKitchenOrderTicketItemDto
{
    [Required]
    public int MenuItemId { get; set; }

    public decimal Quantity { get; set; }

    public string? Notes { get; set; }
}