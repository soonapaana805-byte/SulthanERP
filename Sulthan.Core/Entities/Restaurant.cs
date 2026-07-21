namespace Sulthan.Core.Entities;

public class Restaurant : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string GSTNumber { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}