namespace Sulthan.Core.DTOs.Billing;

public class BillResponseDto
{
    public string BillNumber { get; set; } = string.Empty;

    public string OrderType { get; set; } = string.Empty;

    public string? TableNumber { get; set; }

    public string? CustomerName { get; set; }

    public string CaptainName { get; set; } = string.Empty;

    public DateTime BillDate { get; set; }

    public decimal SubTotal { get; set; }

    public decimal Discount { get; set; }

    public decimal Tax { get; set; }

    public decimal GrandTotal { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal BalanceAmount { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public List<BillItemDto> Items { get; set; } = new();
}