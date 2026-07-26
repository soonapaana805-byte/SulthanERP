using System.Text;
using Sulthan.Core.DTOs.Billing;
using Sulthan.Core.Interfaces;

namespace Sulthan.Infrastructure.Services;

public class ReceiptFormatter : IReceiptFormatter
{
    private readonly ISettingsRepository _settingsRepository;

    public ReceiptFormatter(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public async Task<string> Generate80mmReceiptAsync(PrintBillDto bill)
    {
        var settings = await _settingsRepository.GetAsync();

        var sb = new StringBuilder();

        var hotelName = settings?.ShopName ?? bill.HotelName;
        var address = settings?.Address ?? bill.Address;
        var footer = settings?.FooterMessage ?? "Thank You • Visit Again";

        sb.AppendLine(Center(hotelName));
        sb.AppendLine(Center(address));

        sb.AppendLine(new string('-', 48));

        sb.AppendLine($"Bill : {bill.BillNumber}   {bill.BillDate:dd-MM-yyyy hh:mm tt}");
        sb.AppendLine($"Table: {bill.TableNumber}   Captain: {bill.CaptainName}");
        sb.AppendLine($"Cashier: {bill.CashierName}");

        sb.AppendLine(new string('-', 48));

        sb.AppendLine(
            $"{PadRight("Qty", 4)}" +
            $"{PadRight("Item", 24)}" +
            $"{PadLeft("Rate", 8)}" +
            $"{PadLeft("Total", 10)}");

        sb.AppendLine(new string('-', 48));

        foreach (var item in bill.Items)
        {
            sb.AppendLine(
                $"{PadRight(item.Quantity.ToString(), 4)}" +
                $"{PadRight(item.ItemName, 24)}" +
                $"{PadLeft(item.Price.ToString("0.00"), 8)}" +
                $"{PadLeft(item.Total.ToString("0.00"), 10)}");
        }

        sb.AppendLine(new string('-', 48));

        sb.AppendLine($"{PadRight("Subtotal", 38)}{PadLeft(bill.SubTotal.ToString("0.00"), 10)}");

        if (bill.Discount > 0)
            sb.AppendLine($"{PadRight("Discount", 38)}{PadLeft(bill.Discount.ToString("0.00"), 10)}");

        if (settings?.ShowTaxOnCustomerBill == true)
            sb.AppendLine($"{PadRight("Tax", 38)}{PadLeft(bill.Tax.ToString("0.00"), 10)}");

        sb.AppendLine(new string('-', 48));

        sb.AppendLine($"{PadRight("TOTAL", 38)}{PadLeft(bill.GrandTotal.ToString("0.00"), 10)}");

        sb.AppendLine($"{PadRight(bill.PaymentMethod, 38)}{PadLeft(bill.PaidAmount.ToString("0.00"), 10)}");

        if (bill.BalanceAmount > 0)
            sb.AppendLine($"{PadRight("Balance", 38)}{PadLeft(bill.BalanceAmount.ToString("0.00"), 10)}");

        sb.AppendLine(new string('-', 48));

        sb.AppendLine(Center(footer));

        return sb.ToString();
    }


    public async Task<string> Generate58mmReceiptAsync(PrintBillDto bill)
    {
        return await Generate80mmReceiptAsync(bill);
    }


    private static string Center(string text)
    {
        const int width = 48;

        if (string.IsNullOrWhiteSpace(text))
            return "";

        if (text.Length >= width)
            return text;

        return new string(' ', (width - text.Length) / 2) + text;
    }


    private static string PadRight(string text, int width)
    {
        if (text.Length > width)
            text = text[..width];

        return text.PadRight(width);
    }


    private static string PadLeft(string text, int width)
    {
        if (text.Length > width)
            text = text[..width];

        return text.PadLeft(width);
    }
}