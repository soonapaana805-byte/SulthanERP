using System.ComponentModel.DataAnnotations;
using Sulthan.Core.DTOs.Auth;
using Sulthan.Core.DTOs.Orders;
using Sulthan.Core.Enums;

namespace Sulthan.Core.DTOs.Checkout;

public sealed class CreateCheckoutDto
{
    [EnumDataType(typeof(OrderType), ErrorMessage = "Invalid order type.")]
    public OrderType OrderType { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Invalid dining table.")]
    public int? DiningTableId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Invalid customer.")]
    public int? CustomerId { get; set; }

    [StringLength(150, ErrorMessage = "Customer name cannot exceed 150 characters.")]
    public string? CustomerName { get; set; }

    [Range(0, 99999999, ErrorMessage = "Discount cannot be negative.")]
    public decimal Discount { get; set; }

    public ManagerApprovalDto? DiscountApproval { get; set; }

    [Range(0, 99999999, ErrorMessage = "Tax cannot be negative.")]
    public decimal Tax { get; set; }

    [Required(ErrorMessage = "Order items are required.")]
    [MinLength(1, ErrorMessage = "At least one order item is required.")]
    public List<AddOrderItemDto> Items { get; set; } = [];

    [Required(ErrorMessage = "At least one payment is required.")]
    [MinLength(1, ErrorMessage = "At least one payment is required.")]
    [MaxLength(2, ErrorMessage = "A checkout can have at most two payment lines.")]
    public List<CheckoutPaymentDto> Payments { get; set; } = [];
}

public sealed class CheckoutPaymentDto
{
    [EnumDataType(typeof(PaymentMode), ErrorMessage = "Invalid payment method.")]
    public PaymentMode PaymentMethod { get; set; }

    [Range(0.01, 99999999, ErrorMessage = "Payment amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Range(0.01, 99999999, ErrorMessage = "Tendered amount must be greater than zero.")]
    public decimal? TenderedAmount { get; set; }

    [StringLength(100, ErrorMessage = "Transaction number cannot exceed 100 characters.")]
    public string? TransactionNumber { get; set; }
}
