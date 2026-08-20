using Sulthan.Core.Enums;

namespace Sulthan.Core.DTOs.Billing;

public sealed class BillLifecycleDto
{
    public int OrderId { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public string? KitchenTicketNumber { get; set; }
    public OrderType OrderType { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public string? TableNumber { get; set; }
    public string? TableStatus { get; set; }
    public string? CustomerName { get; set; }
    public decimal GrandTotal { get; set; }
    public bool CanCancel { get; set; }
    public bool CanVoid { get; set; }
    public List<BillItemDto> Items { get; set; } = [];
}
