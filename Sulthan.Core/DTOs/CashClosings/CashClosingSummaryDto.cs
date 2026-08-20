namespace Sulthan.Core.DTOs.CashClosings;

/// <summary>
/// Current-day collection figures and, once recorded, the cashier's closing snapshot.
/// </summary>
public sealed class CashClosingSummaryDto
{
    public DateOnly BusinessDate { get; set; }

    public decimal ExpectedCash { get; set; }

    public decimal CardCollection { get; set; }

    public decimal UpiCollection { get; set; }

    public decimal TotalCollection { get; set; }

    public bool IsClosed { get; set; }

    public decimal? CountedCash { get; set; }

    public decimal? Variance { get; set; }

    public string? Notes { get; set; }

    public DateTime? ClosedOn { get; set; }
}
