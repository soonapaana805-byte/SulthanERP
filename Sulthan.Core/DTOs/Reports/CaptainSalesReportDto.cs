namespace Sulthan.Core.DTOs.Reports;

public class CaptainSalesReportDto
{
    public int CaptainId { get; set; }

    public string CaptainName { get; set; } = string.Empty;

    public int TotalBills { get; set; }

    public decimal TotalSales { get; set; }
}