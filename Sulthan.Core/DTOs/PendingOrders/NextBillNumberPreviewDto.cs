namespace Sulthan.Core.DTOs.PendingOrders;

/// <summary>
/// A display-only bill number preview. Reading it never reserves or increments the bill counter.
/// </summary>
public sealed class NextBillNumberPreviewDto
{
    public string BillNumber { get; set; } = string.Empty;

    public bool IsPreview { get; set; } = true;
}
