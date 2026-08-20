namespace Sulthan.Core.Entities;

/// <summary>
/// Immutable end-of-day cash-count snapshot for one cashier. It does not prevent new sales.
/// </summary>
public sealed class CashClosing : BaseEntity
{
    public int CashierId { get; set; }

    public User Cashier { get; set; } = null!;

    /// <summary>
    /// Restaurant-local business date.
    /// </summary>
    public DateOnly BusinessDate { get; set; }

    /// <summary>
    /// Cash sales expected from paid payment allocations at close time.
    /// </summary>
    public decimal ExpectedCash { get; set; }

    public decimal CardCollection { get; set; }

    public decimal UpiCollection { get; set; }

    public decimal TotalCollection { get; set; }

    public decimal CountedCash { get; set; }

    /// <summary>
    /// Counted cash minus expected cash. A negative value is a shortage.
    /// </summary>
    public decimal Variance { get; set; }

    public string? Notes { get; set; }

    public DateTime ClosedOn { get; set; }
}
