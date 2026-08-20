using CommunityToolkit.Mvvm.ComponentModel;

namespace SulthanERP.Cashier.ViewModels;

/// <summary>
/// One cash denomination and the quantity counted in the till.
/// </summary>
public partial class CashDenominationViewModel : ObservableObject
{
    public CashDenominationViewModel(int value)
    {
        Value = value;
    }

    public int Value { get; }

    [ObservableProperty] private string countText = string.Empty;

    public int Count => int.TryParse(CountText, out var count) && count >= 0 ? count : 0;

    public decimal Total => Value * Count;

    partial void OnCountTextChanged(string value)
    {
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(nameof(Total));
    }
}
