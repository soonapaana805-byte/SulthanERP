using System.ComponentModel.DataAnnotations;
using Sulthan.Core.DTOs.Orders;

namespace Sulthan.Core.DTOs.CaptainOrders;

/// <summary>
/// Adds items to an occupied table and sends only those new items as a new KOT.
/// </summary>
public sealed class AddCaptainOrderItemsDto
{
    [Required(ErrorMessage = "Order items are required.")]
    [MinLength(1, ErrorMessage = "At least one order item is required.")]
    public List<AddOrderItemDto> Items { get; set; } = [];
}
