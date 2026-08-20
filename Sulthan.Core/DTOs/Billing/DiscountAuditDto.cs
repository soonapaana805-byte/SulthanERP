namespace Sulthan.Core.DTOs.Billing;

public sealed class DiscountAuditDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal PreviousDiscount { get; set; }
    public decimal ApprovedDiscount { get; set; }
    public decimal GrandTotal { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int RequestedByUserId { get; set; }
    public string RequestedByUserName { get; set; } = string.Empty;
    public int ApprovedByUserId { get; set; }
    public string ApprovedByUserName { get; set; } = string.Empty;
    public DateTime ApprovedOn { get; set; }
}
