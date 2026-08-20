using Sulthan.Core.Enums;

namespace Sulthan.Core.DTOs.Checkout;

public sealed class CheckoutResponseDto
{
    public int OrderId { get; set; }

    public string BillNumber { get; set; } = string.Empty;

    public string KitchenTicketNumber { get; set; } = string.Empty;

    public DateTime BillDate { get; set; }

    public decimal SubTotal { get; set; }

    public decimal Discount { get; set; }

    public decimal Tax { get; set; }

    public decimal GrandTotal { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal TenderedAmount { get; set; }

    public decimal ChangeAmount { get; set; }

    public PaymentMode PaymentMethod { get; set; }

    public List<CheckoutPaymentResponseDto> Payments { get; set; } = [];
}

public sealed class CheckoutPaymentResponseDto
{
    public PaymentMode PaymentMethod { get; set; }

    public decimal Amount { get; set; }

    public decimal TenderedAmount { get; set; }

    public decimal ChangeAmount { get; set; }

    public string? TransactionNumber { get; set; }
}
