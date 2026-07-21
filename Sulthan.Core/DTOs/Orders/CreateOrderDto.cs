using System.ComponentModel.DataAnnotations;
using Sulthan.Core.Enums;

namespace Sulthan.Core.DTOs.Orders;

public class CreateOrderDto
{
    [Required]
    public OrderType OrderType { get; set; }

    public int? DiningTableId { get; set; }

    public int? CustomerId { get; set; }

    [Required]
    public int UserId { get; set; }

    [MinLength(1, ErrorMessage = "At least one item is required.")]
    public List<AddOrderItemDto> Items { get; set; } = new();
}