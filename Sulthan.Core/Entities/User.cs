using Sulthan.Core.Enums;

namespace Sulthan.Core.Entities;


public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }
}