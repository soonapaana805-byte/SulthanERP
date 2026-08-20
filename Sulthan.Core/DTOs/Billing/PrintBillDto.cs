using System;

namespace Sulthan.Core.DTOs.Billing;

public class PrintBillDto
{
    public string DocumentTitle { get; set; } = string.Empty;

    public bool IsReprint { get; set; }

    public string HotelName { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string BillNumber { get; set; } = string.Empty;

    public DateTime BillDate { get; set; }

    public string TableNumber { get; set; } = string.Empty;

    public string? CustomerName { get; set; }

    public string CaptainName { get; set; } = string.Empty;

    public string CashierName { get; set; } = string.Empty;

    public string OrderType { get; set; } = string.Empty;

    public decimal SubTotal { get; set; }

    public decimal Discount { get; set; }

    // Stored internally. Printed only when enabled.
    public decimal Tax { get; set; }

    public bool ShowTax { get; set; } = false;

    public decimal GrandTotal { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal BalanceAmount { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public List<BillPaymentDto> Payments { get; set; } = new();

    public List<BillItemDto> Items { get; set; } = new();
}
