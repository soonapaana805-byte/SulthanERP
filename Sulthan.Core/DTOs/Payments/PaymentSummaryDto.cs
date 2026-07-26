namespace Sulthan.Core.DTOs.Payments;

public class PaymentSummaryDto
{
    public int TotalPayments { get; set; }

    public decimal TotalCash { get; set; }

    public decimal TotalCard { get; set; }

    public decimal TotalUpi { get; set; }

    public decimal TotalMixed { get; set; }

    public decimal GrandTotal { get; set; }
}