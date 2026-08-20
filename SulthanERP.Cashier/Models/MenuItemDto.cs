using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace SulthanERP.Cashier.Models;

public sealed partial class MenuItemDto : ObservableObject
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? TamilName { get; set; }
    public int CategoryId { get; set; }
    public decimal ACPrice { get; set; }
    public decimal NonACPrice { get; set; }
    public decimal ParcelPrice { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsParcelAvailable { get; set; }

    [JsonIgnore]
    [ObservableProperty] private decimal displayPrice;

    public string? DisplayTamilName => string.Equals(Name?.Trim(), TamilName?.Trim(), StringComparison.OrdinalIgnoreCase)
        ? null
        : TamilName;
}

public sealed class CategoryDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool IsActive { get; set; }
}
