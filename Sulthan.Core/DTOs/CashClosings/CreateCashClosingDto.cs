using System.ComponentModel.DataAnnotations;

namespace Sulthan.Core.DTOs.CashClosings;

/// <summary>
/// The cashier's physical cash count. Collection figures are always calculated by the server.
/// </summary>
public sealed class CreateCashClosingDto
{
    [Range(0, 99999999, ErrorMessage = "Counted cash cannot be negative.")]
    public decimal CountedCash { get; set; }

    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }
}
