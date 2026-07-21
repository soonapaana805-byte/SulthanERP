namespace Sulthan.Core.Entities;

public class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;
}