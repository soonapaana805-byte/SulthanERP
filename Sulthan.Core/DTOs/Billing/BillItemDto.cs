namespace Sulthan.Core.DTOs.Billing;

public class BillItemDto
{
    public int MenuItemId { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public decimal Total { get; set; }
}