using System.ComponentModel.DataAnnotations;
using Sulthan.Core.Enums;

namespace Sulthan.Core.DTOs.Orders;

public class UpdateOrderDto
{
    [Required(ErrorMessage = "Order Status is required.")]
    public OrderStatus OrderStatus { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Invalid Dining Table.")]
    public int? DiningTableId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Invalid Customer.")]
    public int? CustomerId { get; set; }

    [Required(ErrorMessage = "Order Items are required.")]
    [MinLength(1, ErrorMessage = "At least one item is required.")]
    public List<UpdateOrderItemDto> Items { get; set; } = new();
}