using Sulthan.Core.Enums;

namespace Sulthan.Core.DTOs.Orders;

public class OrderResponseDto
{
    public int Id { get; set; }

    public string BillNumber { get; set; } = string.Empty;

    public OrderType OrderType { get; set; }

    public OrderStatus OrderStatus { get; set; }

    public int? DiningTableId { get; set; }

    public string? TableNumber { get; set; }

    public int? CustomerId { get; set; }

    public string? CustomerName { get; set; }

    public int UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public DateTime CreatedOn { get; set; }

    public List<AddOrderItemDto> Items { get; set; } = new();
}