namespace SulthanERP.Api.Printing;

public sealed class CashierPrintingOptions
{
    public const string SectionName = "CashierPrinting";

    public bool Enabled { get; set; } = true;

    /// <summary>File for safe testing, or Windows for a configured printer queue.</summary>
    public string Mode { get; set; } = "File";

    public string SpoolDirectory { get; set; } = "PrintSpool/Cashier";

    public string PrinterName { get; set; } = "Cash Counter Printer";

    public int PollIntervalSeconds { get; set; } = 3;

    public int RetryIntervalSeconds { get; set; } = 60;

    /// <summary>Zero means retry forever.</summary>
    public int MaxRetryAttempts { get; set; }

    public int ProcessingTimeoutSeconds { get; set; } = 120;
}
