using System.ComponentModel.DataAnnotations;

namespace Sulthan.Core.DTOs.Orders;

public class UpdateOrderItemDto
{
    [Required]
    public int MenuItemId { get; set; }

    [Required]
    [Range(1, 100)]
    public int Quantity { get; set; }

    public string? Notes { get; set; }
}