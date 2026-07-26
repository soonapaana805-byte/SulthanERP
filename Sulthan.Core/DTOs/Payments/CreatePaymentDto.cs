using System.ComponentModel.DataAnnotations;
using Sulthan.Core.Enums;

namespace Sulthan.Core.DTOs.Payments;

public class CreatePaymentDto
{
    [Required(ErrorMessage = "Order is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid Order Id.")]
    public int OrderId { get; set; }

    [Required(ErrorMessage = "Payment Method is required.")]
    public PaymentMode PaymentMethod { get; set; }

    [Required(ErrorMessage = "Paid Amount is required.")]
    [Range(0.01, 99999999, ErrorMessage = "Paid Amount must be greater than zero.")]
    public decimal PaidAmount { get; set; }

    [StringLength(100, ErrorMessage = "Transaction Number cannot exceed 100 characters.")]
    public string? TransactionNumber { get; set; }
}