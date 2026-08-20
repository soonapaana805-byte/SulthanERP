using System.Globalization;
using System.Windows;
using SulthanERP.Cashier.Services;

namespace SulthanERP.Cashier.Views;

public partial class ReceiptWindow : Window
{
    private readonly ApiService _api; private readonly int _orderId; private string? _billNumber;
    public string BillLabel => string.IsNullOrWhiteSpace(_billNumber) ? "Bill being generated" : $"Bill no: {_billNumber}";
    public string OrderLabel => $"Order no: {_orderId}";
    public string TotalLabel { get; }
    public string Status { get; private set; } = "Payment recorded successfully.";
    public ReceiptWindow(ApiService api, int orderId, decimal total, string? billNumber = null) { _api = api; _orderId = orderId; _billNumber = billNumber; TotalLabel = total.ToString("C", CultureInfo.GetCultureInfo("en-IN")); InitializeComponent(); DataContext = this; Loaded += LoadReceiptAsync; }
    private async void LoadReceiptAsync(object sender, RoutedEventArgs e) { if (!string.IsNullOrWhiteSpace(_billNumber)) { Status = "Receipt is ready to print."; DataContext = null; DataContext = this; return; } try { var response = await _api.GetAsync($"Billing/{_orderId}"); if (response.IsSuccessful) { _billNumber = ApiService.ReadString(response.Content, "billNumber", "invoiceNumber", "number"); Status = string.IsNullOrWhiteSpace(_billNumber) ? "Receipt is ready." : "Receipt is ready to print."; } else Status = "Payment is complete. Receipt details are unavailable."; DataContext = null; DataContext = this; } catch { Status = "Payment is complete. Receipt details are unavailable."; DataContext = null; DataContext = this; } }
    private async void PrintClick(object sender, RoutedEventArgs e) { if (string.IsNullOrWhiteSpace(_billNumber)) { Status = "Bill number is still unavailable."; DataContext = null; DataContext = this; return; } var response = await _api.GetAsync($"Billing/print/{Uri.EscapeDataString(_billNumber)}"); Status = response.IsSuccessful ? "Print request sent." : "Could not send print request."; DataContext = null; DataContext = this; }
    private void CloseClick(object sender, RoutedEventArgs e) => Close();
}
