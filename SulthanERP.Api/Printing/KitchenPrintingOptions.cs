namespace SulthanERP.Api.Printing;

public sealed class KitchenPrintingOptions
{
    public const string SectionName = "KitchenPrinting";

    public bool Enabled { get; set; } = true;

    /// <summary>File for safe testing, or Windows for a configured printer queue.</summary>
    public string Mode { get; set; } = "File";

    public string SpoolDirectory { get; set; } = "PrintSpool/Kitchen";

    public string DefaultPrinterName { get; set; } = "Kitchen Printer";

    public Dictionary<string, string> PrinterMappings { get; set; } = new();

    public int PaperWidthCharacters { get; set; } = 42;

    public int PollIntervalSeconds { get; set; } = 3;

    public int RetryIntervalSeconds { get; set; } = 60;

    /// <summary>Zero means retry forever.</summary>
    public int MaxRetryAttempts { get; set; }

    public int ProcessingTimeoutSeconds { get; set; } = 120;
}
