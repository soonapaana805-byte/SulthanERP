using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using Sulthan.Core.DTOs.PendingOrders;
using Sulthan.Core.DTOs.Reports;
using SulthanERP.Cashier.Services;

namespace SulthanERP.Cashier.ViewModels;

/// <summary>
/// Read-only daily cashier summary. It does not perform a financial day-close or lock any sales.
/// </summary>
public partial class DailySalesViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly Action _returnToPos;
    private readonly Action _showCashClosing;

    [ObservableProperty] private DateTime selectedDate = DateTime.Today;
    [ObservableProperty] private DailySalesReportDto report = new();
    [ObservableProperty] private int pendingPhoneBillCount;
    [ObservableProperty] private decimal pendingPhoneAmount;
    [ObservableProperty] private string statusMessage = "Loading daily sales...";
    [ObservableProperty] private bool isLoading;

    public DailySalesViewModel(ApiService api, Action returnToPos, Action showCashClosing)
    {
        _api = api;
        _returnToPos = returnToPos;
        _showCashClosing = showCashClosing;
        _ = LoadAsync();
    }

    [RelayCommand]
    private void BackToPos() => _returnToPos();

    [RelayCommand]
    private void ShowCashClosing() => _showCashClosing();

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsLoading)
            return;

        try
        {
            IsLoading = true;
            StatusMessage = "Loading daily sales...";
            var date = SelectedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            var dailyTask = _api.GetAsync($"Reports/daily?date={Uri.EscapeDataString(date)}");
            var pendingTask = _api.GetAsync("PendingOrders");
            await Task.WhenAll(dailyTask, pendingTask);

            if (!dailyTask.Result.IsSuccessful)
            {
                StatusMessage = ApiService.ReadString(dailyTask.Result.Content, "message")
                    ?? $"Daily report could not be loaded: {(int)dailyTask.Result.StatusCode}";
                return;
            }

            Report = JsonConvert.DeserializeObject<DailySalesReportDto>(dailyTask.Result.Content ?? string.Empty) ?? new DailySalesReportDto
            {
                Date = SelectedDate
            };

            if (pendingTask.Result.IsSuccessful)
            {
                var pendingOrders = JsonConvert.DeserializeObject<List<PendingOrderDto>>(pendingTask.Result.Content ?? "[]") ?? [];
                PendingPhoneBillCount = pendingOrders.Count;
                PendingPhoneAmount = pendingOrders.Sum(x => x.GrandTotal);
            }
            else
            {
                PendingPhoneBillCount = 0;
                PendingPhoneAmount = 0m;
            }

            StatusMessage = $"Sales summary for {SelectedDate:dd MMM yyyy}. Open phone bills are current balances.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Unable to load daily sales: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
