namespace Sulthan.Core.DTOs.Billing;

public class PrintReceiptResponseDto
{
    public string ReceiptText { get; set; } = string.Empty;

    public string PrinterType { get; set; } = "80MM";

    public DateTime GeneratedOn { get; set; } = DateTime.Now;
}