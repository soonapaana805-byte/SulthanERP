using System.ComponentModel.DataAnnotations;
using Sulthan.Core.Enums;

namespace Sulthan.Core.DTOs.Orders;

public class UpdateOrderDto
{
    [Required]
    public OrderStatus OrderStatus { get; set; }

    public int? DiningTableId { get; set; }

    public int? CustomerId { get; set; }

    public List<UpdateOrderItemDto> Items { get; set; } = new();
}