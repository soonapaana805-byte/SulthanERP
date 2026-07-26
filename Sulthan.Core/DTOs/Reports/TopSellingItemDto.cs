namespace Sulthan.Core.DTOs.Reports;

public class TopSellingItemDto
{
    public int MenuItemId { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public int QuantitySold { get; set; }

    public decimal TotalSales { get; set; }
}