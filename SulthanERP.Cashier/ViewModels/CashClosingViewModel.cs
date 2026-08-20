using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using SulthanERP.Cashier.Models;
using SulthanERP.Cashier.Services;

namespace SulthanERP.Cashier.ViewModels;

/// <summary>
/// Cashier-facing, today-only cash count. It records a snapshot and never locks POS sales.
/// </summary>
public partial class CashClosingViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly IUserDialogService _dialogService;
    private readonly Action _returnToSummary;

    [ObservableProperty] private CashClosingSummaryModel summary = new();
    [ObservableProperty] private string actualCashText = string.Empty;
    [ObservableProperty] private string notes = string.Empty;
    [ObservableProperty] private string statusMessage = "Loading today's collection...";
    [ObservableProperty] private string statusBrush = "#475569";
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isSaving;

    public ObservableCollection<CashDenominationViewModel> Denominations { get; } = [];

    public CashClosingViewModel(
        ApiService api,
        IUserDialogService dialogService,
        Action returnToSummary)
    {
        _api = api;
        _dialogService = dialogService;
        _returnToSummary = returnToSummary;

        foreach (var value in new[] { 1, 2, 5, 10, 20, 50, 100, 200, 500 })
        {
            var denomination = new CashDenominationViewModel(value);
            denomination.PropertyChanged += OnDenominationPropertyChanged;
            Denominations.Add(denomination);
        }

        _ = LoadAsync();
    }

    public bool IsClosed => Summary.IsClosed;

    public bool CanEditClosing => !IsClosed && !IsLoading && !IsSaving;

    public decimal? ActualCash
    {
        get
        {
            return decimal.TryParse(
                ActualCashText,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out var amount) && amount >= 0m
                ? amount
                : null;
        }
    }

    public bool CanRecordClosing => CanEditClosing && ActualCash.HasValue;

    public decimal DenominationTotal => Denominations.Sum(x => x.Total);

    public decimal Variance => ActualCash is decimal actual
        ? actual - Summary.ExpectedCash
        : 0m;

    public string VarianceText
    {
        get
        {
            if (!ActualCash.HasValue)
                return "Enter actual cash to calculate the variance.";

            if (Variance == 0m)
                return "Cash matches the expected amount.";

            return Variance > 0m
                ? $"Excess: ₹ {Variance:N2}"
                : $"Shortage: ₹ {Math.Abs(Variance):N2}";
        }
    }

    public string VarianceBrush => !ActualCash.HasValue
        ? "#64748B"
        : Variance < 0m ? "#B91C1C" : "#15803D";

    public string BusinessDateText => Summary.BusinessDate.ToString("dd MMM yyyy", CultureInfo.CurrentCulture);

    public string ClosingStatusText => IsClosed
        ? $"Recorded {Summary.ClosedOn:dd MMM yyyy, hh:mm tt}"
        : "Not recorded yet";

    public string ClosingStatusBrush => IsClosed ? "#15803D" : "#B45309";

    [RelayCommand]
    private void BackToSummary() => _returnToSummary();

    [RelayCommand]
    private void ClearDenominations()
    {
        foreach (var denomination in Denominations)
            denomination.CountText = string.Empty;

        ActualCashText = string.Empty;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsLoading || IsSaving)
            return;

        try
        {
            IsLoading = true;
            SetStatus("Loading today's cashier collection...", "#475569");

            var response = await _api.GetAsync("CashClosings/today");
            if (!response.IsSuccessful)
            {
                SetStatus(ReadError(response.Content, response.StatusCode.ToString()), "#B91C1C");
                return;
            }

            Summary = JsonConvert.DeserializeObject<CashClosingSummaryModel>(response.Content ?? string.Empty)
                      ?? new CashClosingSummaryModel();
            ActualCashText = Summary.CountedCash?.ToString("0.##", CultureInfo.CurrentCulture) ?? string.Empty;
            Notes = Summary.Notes ?? string.Empty;
            SetStatus(
                IsClosed
                    ? "Today's cash closing has been recorded. This is an immutable collection snapshot."
                    : "Enter the physical cash count and record today's closing.",
                IsClosed ? "#15803D" : "#475569");
        }
        catch (Exception ex)
        {
            SetStatus($"Unable to load cash closing: {ex.Message}", "#B91C1C");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RecordClosingAsync()
    {
        if (!CanEditClosing)
            return;

        if (!ActualCash.HasValue)
        {
            SetStatus("Enter a valid actual cash amount before recording the closing.", "#B91C1C");
            return;
        }

        try
        {
            IsSaving = true;
            SetStatus("Recording today's cash closing...", "#475569");

            var response = await _api.PostAsync("CashClosings", new
            {
                countedCash = ActualCash.Value,
                notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
            });

            if (!response.IsSuccessful)
            {
                SetStatus(ReadError(response.Content, response.StatusCode.ToString()), "#B91C1C");
                return;
            }

            Summary = JsonConvert.DeserializeObject<CashClosingSummaryModel>(response.Content ?? string.Empty)
                      ?? throw new InvalidOperationException("The server returned an empty cash closing response.");
            ActualCashText = Summary.CountedCash?.ToString("0.##", CultureInfo.CurrentCulture) ?? ActualCashText;
            Notes = Summary.Notes ?? string.Empty;
            SetStatus("Today's cash closing was recorded successfully.", "#15803D");

            _dialogService.ShowInformation(
                $"Cash closing recorded.\n\nExpected cash: ₹ {Summary.ExpectedCash:N2}\nActual cash: ₹ {Summary.CountedCash ?? 0m:N2}\nVariance: ₹ {Summary.Variance ?? 0m:N2}",
                "Cash closing recorded");
        }
        catch (Exception ex)
        {
            SetStatus($"Cash closing could not be recorded: {ex.Message}", "#B91C1C");
        }
        finally
        {
            IsSaving = false;
        }
    }

    partial void OnActualCashTextChanged(string value)
    {
        OnPropertyChanged(nameof(ActualCash));
        OnPropertyChanged(nameof(CanRecordClosing));
        OnPropertyChanged(nameof(Variance));
        OnPropertyChanged(nameof(VarianceText));
        OnPropertyChanged(nameof(VarianceBrush));
    }

    partial void OnSummaryChanged(CashClosingSummaryModel value)
    {
        OnPropertyChanged(nameof(IsClosed));
        OnPropertyChanged(nameof(CanEditClosing));
        OnPropertyChanged(nameof(CanRecordClosing));
        OnPropertyChanged(nameof(Variance));
        OnPropertyChanged(nameof(VarianceText));
        OnPropertyChanged(nameof(VarianceBrush));
        OnPropertyChanged(nameof(BusinessDateText));
        OnPropertyChanged(nameof(ClosingStatusText));
        OnPropertyChanged(nameof(ClosingStatusBrush));
    }

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanEditClosing));
        OnPropertyChanged(nameof(CanRecordClosing));
    }

    partial void OnIsSavingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanEditClosing));
        OnPropertyChanged(nameof(CanRecordClosing));
    }

    private void OnDenominationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(CashDenominationViewModel.CountText))
            return;

        OnPropertyChanged(nameof(DenominationTotal));
        ActualCashText = DenominationTotal.ToString("0.##", CultureInfo.CurrentCulture);
    }

    private void SetStatus(string message, string brush)
    {
        StatusMessage = message;
        StatusBrush = brush;
    }

    private static string ReadError(string? content, string fallback)
    {
        try
        {
            return ApiService.ReadString(content, "message") ?? $"Request failed: {fallback}";
        }
        catch
        {
            return $"Request failed: {fallback}";
        }
    }
}
