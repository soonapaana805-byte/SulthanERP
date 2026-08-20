namespace SulthanERP.Cashier.Models;

/// <summary>
/// Client model for the signed-in cashier's today-only cash closing summary.
/// </summary>
public sealed class CashClosingSummaryModel
{
    public DateTime BusinessDate { get; set; } = DateTime.Today;

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
