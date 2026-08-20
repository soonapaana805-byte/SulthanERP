using System.ComponentModel.DataAnnotations;
using Sulthan.Core.DTOs.Orders;

namespace Sulthan.Core.DTOs.CaptainOrders;

/// <summary>
/// Starts a dine-in order and sends its first KOT. Pricing is always resolved by the server.
/// </summary>
public sealed class CreateCaptainOrderDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Invalid dining table.")]
    public int DiningTableId { get; set; }

    [StringLength(150, ErrorMessage = "Customer name cannot exceed 150 characters.")]
    public string? CustomerName { get; set; }

    [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters.")]
    public string? Remarks { get; set; }

    [Required(ErrorMessage = "Order items are required.")]
    [MinLength(1, ErrorMessage = "At least one order item is required.")]
    public List<AddOrderItemDto> Items { get; set; } = [];
}
