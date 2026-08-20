using System.ComponentModel.DataAnnotations;
using Sulthan.Core.Enums;

namespace Sulthan.Core.DTOs.Orders;

public class CreateOrderDto
{
    [Required(ErrorMessage = "Order Type is required.")]
    public OrderType OrderType { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Invalid Dining Table.")]
    public int? DiningTableId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Invalid Customer.")]
    public int? CustomerId { get; set; }

    [Required(ErrorMessage = "Captain is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid Captain.")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "Order Items are required.")]
    [MinLength(1, ErrorMessage = "At least one item is required.")]
    public List<AddOrderItemDto> Items { get; set; } = new();
}