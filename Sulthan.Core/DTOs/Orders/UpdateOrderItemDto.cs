using System.ComponentModel.DataAnnotations;

namespace Sulthan.Core.DTOs.Orders;

public class UpdateOrderItemDto
{
    [Required(ErrorMessage = "Menu Item is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid Menu Item.")]
    public int MenuItemId { get; set; }

    [Required(ErrorMessage = "Quantity is required.")]
    [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100.")]
    public int Quantity { get; set; }

    [StringLength(250, ErrorMessage = "Notes cannot exceed 250 characters.")]
    public string? Notes { get; set; }
}