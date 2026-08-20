using Sulthan.Core.DTOs.Orders.Response;
using Sulthan.Core.DTOs.Payments.Response;
using Sulthan.Core.Enums;

namespace Sulthan.Core.DTOs.Payments;

public class PaymentResponseDto
{
    public int Id { get; set; }

    public OrderSummaryDto? Order { get; set; }

    public UserSummaryDto? Cashier { get; set; }

    public decimal BillAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal GrandTotal { get; set; }

    public PaymentMode PaymentMethod { get; set; }

    public PaymentStatus PaymentStatus { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal BalanceAmount { get; set; }

    public string? TransactionNumber { get; set; }

    public DateTime PaymentDate { get; set; }
}