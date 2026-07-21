namespace Sulthan.Core.Entities;

public class Settings : BaseEntity
{
    public string ShopName { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;
}