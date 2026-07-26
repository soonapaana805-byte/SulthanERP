using Sulthan.Core.DTOs.Orders.Response;

namespace Sulthan.Core.DTOs.Orders.Response;

public class OrderDetailsResponseDto
{
    public int Id { get; set; }

    public string BillNumber { get; set; } = string.Empty;

    public string OrderType { get; set; } = string.Empty;

    public string BillStatus { get; set; } = string.Empty;

    public TableSummaryDto? Table { get; set; }

    public UserSummaryDto? Captain { get; set; }

    public decimal SubTotal { get; set; }

    public decimal Discount { get; set; }

    public decimal Tax { get; set; }

    public decimal GrandTotal { get; set; }

    public string? Remarks { get; set; }

    public List<OrderItemResponseDto> Items { get; set; } = new();
}