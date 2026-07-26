namespace Sulthan.Core.DTOs.Reports;

public class CategorySalesReportDto
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public int QuantitySold { get; set; }

    public decimal TotalSales { get; set; }
}