namespace Sulthan.Core.DTOs.Billing;

public sealed class BillPaymentDto
{
    public string PaymentMethod { get; set; } = string.Empty;

    public decimal PaidAmount { get; set; }

    public decimal TenderedAmount { get; set; }

    public decimal ChangeAmount { get; set; }

    public string? TransactionNumber { get; set; }

    public DateTime PaymentDate { get; set; }
}
