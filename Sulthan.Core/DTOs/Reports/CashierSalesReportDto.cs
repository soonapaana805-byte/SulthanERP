namespace Sulthan.Core.DTOs.Reports;

public class CashierSalesReportDto
{
    public int CashierId { get; set; }

    public string CashierName { get; set; } = string.Empty;

    public int TotalBills { get; set; }

    public decimal TotalCollection { get; set; }
}