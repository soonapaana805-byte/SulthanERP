using CommunityToolkit.Mvvm.ComponentModel;
using SulthanERP.Cashier.Services;
namespace SulthanERP.Cashier.ViewModels;
public partial class ShellViewModel : ObservableObject
{
    private readonly ApiService _api = new();
    private readonly IUserDialogService _dialogService = new WpfUserDialogService();
    private PosViewModel? _posViewModel;

    [ObservableProperty] private object currentViewModel = null!;

    public ShellViewModel()
    {
        LoginViewModel? login = null;
        login = new LoginViewModel(_api, ShowPos);
        CurrentViewModel = login;
    }

    private void ShowPos()
    {
        _posViewModel ??= new PosViewModel(_api, _dialogService, ShowKitchenBills, ShowDailySales);
        CurrentViewModel = _posViewModel;
    }

    private void ShowKitchenBills()
    {
        CurrentViewModel = new KitchenBillsViewModel(
            _api,
            ShowPos,
            OpenPendingOrderAsync,
            _dialogService);
    }

    private void ShowDailySales()
    {
        CurrentViewModel = new DailySalesViewModel(_api, ShowPos, ShowCashClosing);
    }

    private void ShowCashClosing()
    {
        CurrentViewModel = new CashClosingViewModel(_api, _dialogService, ShowDailySales);
    }

    private async Task<bool> OpenPendingOrderAsync(int orderId)
    {
        if (_posViewModel is null)
            return false;

        var loaded = await _posViewModel.LoadPendingPhoneOrderAsync(orderId);
        if (loaded)
            CurrentViewModel = _posViewModel;

        return loaded;
    }
}
