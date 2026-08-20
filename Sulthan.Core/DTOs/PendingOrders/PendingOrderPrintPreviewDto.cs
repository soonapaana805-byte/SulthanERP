namespace Sulthan.Core.DTOs.PendingOrders;

public sealed class PendingOrderPrintPreviewDto
{
    public int OrderId { get; set; }

    public string BillNumber { get; set; } = string.Empty;

    public string ReceiptText { get; set; } = string.Empty;
}
