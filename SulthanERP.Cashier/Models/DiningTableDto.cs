namespace SulthanERP.Cashier.Models;

/// <summary>Cashier-facing shape returned by GET /api/DiningTables.</summary>
public sealed class DiningTableDto
{
    public int Id { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public string TableType { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public bool IsAvailable => string.Equals(Status, "Available", StringComparison.OrdinalIgnoreCase);
    public bool IsOccupied => string.Equals(Status, "Occupied", StringComparison.OrdinalIgnoreCase);
    public bool IsBillRequested => string.Equals(Status, "BillRequested", StringComparison.OrdinalIgnoreCase);
    public bool IsPaymentPending => string.Equals(Status, "PaymentPending", StringComparison.OrdinalIgnoreCase);
    public bool IsCleaningPending => string.Equals(Status, "CleaningPending", StringComparison.OrdinalIgnoreCase);
    public bool IsAc => string.Equals(TableType, "AC", StringComparison.OrdinalIgnoreCase);
    public string StatusDisplay => IsAvailable
        ? "AVAILABLE"
        : IsOccupied
            ? "OCCUPIED"
            : IsBillRequested
                ? "BILL REQUESTED"
                : IsPaymentPending
                    ? "PAYMENT PENDING"
                    : IsCleaningPending
                        ? "READY TO CLEAN"
                        : "BOOKED";
    public string DisplayName => string.IsNullOrWhiteSpace(TableType)
        ? TableNumber
        : $"{TableNumber} ({TableType})";
}
