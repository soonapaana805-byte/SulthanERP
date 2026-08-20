using System.ComponentModel.DataAnnotations;
using Sulthan.Core.DTOs.Auth;
using Sulthan.Core.DTOs.Checkout;

namespace Sulthan.Core.DTOs.PendingOrders;

/// <summary>
/// Closes an existing pending order using one payment or a simple two-line split payment.
/// </summary>
public sealed class PendingOrderCheckoutDto
{
    [Range(0, 99999999, ErrorMessage = "Discount cannot be negative.")]
    public decimal? Discount { get; set; }

    public ManagerApprovalDto? DiscountApproval { get; set; }

    [Required(ErrorMessage = "At least one payment is required.")]
    [MinLength(1, ErrorMessage = "At least one payment is required.")]
    [MaxLength(2, ErrorMessage = "A checkout can have at most two payment lines.")]
    public List<CheckoutPaymentDto> Payments { get; set; } = [];
}
