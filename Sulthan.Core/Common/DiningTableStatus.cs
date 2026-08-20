namespace Sulthan.Core.Common;

/// <summary>
/// Persisted lifecycle states for restaurant dining tables.
/// A table may only return to Available after the paid bill has been marked clean.
/// </summary>
public static class DiningTableStatus
{
    public const string Available = "Available";
    public const string BillRequested = "BillRequested";
    public const string PaymentPending = "PaymentPending";
    public const string CleaningPending = "CleaningPending";
    public const string Occupied = "Occupied";
    public const string Reserved = "Reserved";

    public static bool IsAvailable(string? status) =>
        string.Equals(status, Available, StringComparison.OrdinalIgnoreCase);
}
