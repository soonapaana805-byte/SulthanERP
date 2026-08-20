namespace Sulthan.Core.DTOs.Reports;

public class DailySalesReportDto
{
    public DateTime Date { get; set; }

    public int TotalBills { get; set; }

    public decimal TotalSales { get; set; }

    public decimal TotalDiscount { get; set; }

    public decimal TotalTax { get; set; }

    public decimal NetSales { get; set; }

    public decimal CashCollection { get; set; }

    public decimal CardCollection { get; set; }

    public decimal UpiCollection { get; set; }

    public int CancelledBills { get; set; }

    public int VoidedBills { get; set; }

    public decimal VoidedAmount { get; set; }
}
