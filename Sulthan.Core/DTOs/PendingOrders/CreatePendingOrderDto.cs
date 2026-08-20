using System.ComponentModel.DataAnnotations;
using Sulthan.Core.DTOs.Auth;
using Sulthan.Core.DTOs.Orders;
using Sulthan.Core.Enums;

namespace Sulthan.Core.DTOs.PendingOrders;

/// <summary>
/// Creates an unpaid order and sends its KOT to the kitchen workflow.
/// </summary>
public sealed class CreatePendingOrderDto
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

    [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters.")]
    public string? Remarks { get; set; }

    [Required(ErrorMessage = "Order items are required.")]
    [MinLength(1, ErrorMessage = "At least one order item is required.")]
    public List<AddOrderItemDto> Items { get; set; } = [];
}
