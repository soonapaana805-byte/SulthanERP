using System.ComponentModel.DataAnnotations;

namespace Sulthan.Core.DTOs.Auth;

/// <summary>
/// Active Admin credentials used to approve a controlled POS operation.
/// The password is validated in memory and is never persisted.
/// </summary>
public sealed class ManagerApprovalDto
{
    [Required(ErrorMessage = "Manager username is required.")]
    [StringLength(50, ErrorMessage = "Manager username cannot exceed 50 characters.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Manager password is required.")]
    [StringLength(200, ErrorMessage = "Manager password cannot exceed 200 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Approval reason is required.")]
    [StringLength(250, MinimumLength = 3, ErrorMessage = "Approval reason must be between 3 and 250 characters.")]
    public string Reason { get; set; } = string.Empty;
}
