namespace Sulthan.Core.Entities;

/// <summary>Immutable item snapshot printed on a KOT cancellation slip.</summary>
public sealed class KotCancellationAuditItem : BaseEntity
{
    public int KotCancellationAuditId { get; set; }
    public KotCancellationAudit KotCancellationAudit { get; set; } = null!;
    public int MenuItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string KitchenName { get; set; } = "Main Kitchen";
    public decimal CancelledQuantity { get; set; }
}
