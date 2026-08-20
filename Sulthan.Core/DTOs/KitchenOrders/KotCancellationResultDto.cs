namespace Sulthan.Core.DTOs.KitchenOrders;

public sealed class KotCancellationResultDto
{
    public int KitchenOrderTicketId { get; set; }
    public int OrderId { get; set; }
    public string KotNumber { get; set; } = string.Empty;
    public string BillNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal GrandTotal { get; set; }
    public DateTime CancelledOn { get; set; }
}
