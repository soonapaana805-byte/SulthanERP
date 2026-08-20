using System.Text;
using Sulthan.Core.DTOs.Billing;
using Sulthan.Core.Entities;
using Sulthan.Core.Interfaces;

namespace Sulthan.Infrastructure.Services;

public sealed class ReceiptFormatter : IReceiptFormatter
{
    private const int Width58 = 32;
    private const int Width80 = 48;
    private readonly ISettingsRepository _settingsRepository;

    public ReceiptFormatter(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public async Task<string> GenerateReceiptAsync(PrintBillDto bill)
    {
        ArgumentNullException.ThrowIfNull(bill);
        var settings = await _settingsRepository.GetAsync();
        return Is58mm(settings?.PrinterWidth)
            ? Format58mm(bill, settings)
            : Format80mm(bill, settings);
    }

    public async Task<string> Generate80mmReceiptAsync(PrintBillDto bill)
    {
        ArgumentNullException.ThrowIfNull(bill);
        return Format80mm(bill, await _settingsRepository.GetAsync());
    }

    public async Task<string> Generate58mmReceiptAsync(PrintBillDto bill)
    {
        ArgumentNullException.ThrowIfNull(bill);
        return Format58mm(bill, await _settingsRepository.GetAsync());
    }

    private static string Format80mm(PrintBillDto bill, Settings? settings)
    {
        const int quantityWidth = 4;
        const int itemWidth = 23;
        const int rateWidth = 9;
        const int totalWidth = 12;
        var builder = new StringBuilder();
        var divider = new string('-', Width80);
        var pending = IsPaymentPending(bill);

        AppendReceiptHeader(builder, bill, settings, Width80);
        builder.AppendLine(divider);
        AppendCenteredWrapped(
            builder,
            pending ? "CUSTOMER BILL (Pending)" : "CUSTOMER RECEIPT",
            Width80);
        if (bill.IsReprint)
            AppendCenteredWrapped(builder, "*** REPRINT ***", Width80);
        if (pending)
            AppendCenteredWrapped(builder, "*** PAYMENT PENDING ***", Width80);
        builder.AppendLine(divider);

        AppendLabelValue(builder, "Bill", CompactBillNumber(bill.BillNumber), Width80);
        AppendLabelValue(
            builder,
            "Date",
            bill.BillDate.ToString("dd-MM-yyyy hh:mm tt"),
            Width80);
        AppendLabelValue(builder, "Mode", NormalizeOrderMode(bill.OrderType), Width80);
        if (!string.IsNullOrWhiteSpace(bill.TableNumber))
            AppendLabelValue(builder, "Table", bill.TableNumber, Width80);
        AppendStaffDetails(builder, bill, Width80);
        if (!string.IsNullOrWhiteSpace(bill.CustomerName))
            AppendLabelValue(builder, "Customer", bill.CustomerName, Width80);

        builder.AppendLine(divider);
        builder.AppendLine(
            PadRight("Qty", quantityWidth) +
            PadRight("Item", itemWidth) +
            PadLeft("Rate", rateWidth) +
            PadLeft("Total", totalWidth));
        builder.AppendLine(divider);

        foreach (var item in bill.Items)
        {
            var itemLines = Wrap(item.ItemName, itemWidth);
            var quantity = item.Quantity.ToString();
            var rate = FormatMoney(item.Price, settings);
            var total = FormatMoney(item.Total, settings);

            if (quantity.Length > quantityWidth ||
                rate.Length > rateWidth ||
                total.Length > totalWidth)
            {
                foreach (var line in Wrap(item.ItemName, Width80))
                    builder.AppendLine(line);

                AppendLeftRight(
                    builder,
                    $"{quantity} x {rate}",
                    total,
                    Width80);
                continue;
            }

            builder.AppendLine(
                PadRight(quantity, quantityWidth) +
                PadRight(itemLines[0], itemWidth) +
                PadLeft(rate, rateWidth) +
                PadLeft(total, totalWidth));

            foreach (var continuation in itemLines.Skip(1))
            {
                builder.AppendLine(
                    new string(' ', quantityWidth) +
                    PadRight(continuation, itemWidth));
            }
        }

        builder.AppendLine(divider);
        AppendTotals80(builder, bill, settings);
        builder.AppendLine(divider);
        AppendPayments80(builder, bill, settings);
        builder.AppendLine(divider);
        AppendReceiptFooter(builder, settings, Width80);
        return builder.ToString();
    }

    private static string Format58mm(PrintBillDto bill, Settings? settings)
    {
        var builder = new StringBuilder();
        var divider = new string('-', Width58);
        var pending = IsPaymentPending(bill);

        AppendReceiptHeader(builder, bill, settings, Width58);
        builder.AppendLine(divider);
        AppendCenteredWrapped(
            builder,
            pending ? "CUSTOMER BILL (Pending)" : "CUSTOMER RECEIPT",
            Width58);
        if (bill.IsReprint)
            AppendCenteredWrapped(builder, "*** REPRINT ***", Width58);
        if (pending)
            AppendCenteredWrapped(builder, "*** PAYMENT PENDING ***", Width58);
        builder.AppendLine(divider);

        AppendLabelValue(builder, "Bill", CompactBillNumber(bill.BillNumber), Width58);
        AppendLabelValue(
            builder,
            "Date",
            bill.BillDate.ToString("dd-MM-yyyy hh:mm tt"),
            Width58);
        AppendLabelValue(builder, "Mode", NormalizeOrderMode(bill.OrderType), Width58);
        if (!string.IsNullOrWhiteSpace(bill.TableNumber))
            AppendLabelValue(builder, "Table", bill.TableNumber, Width58);
        AppendStaffDetails(builder, bill, Width58);
        if (!string.IsNullOrWhiteSpace(bill.CustomerName))
            AppendLabelValue(builder, "Customer", bill.CustomerName, Width58);

        builder.AppendLine(divider);
        builder.AppendLine("ITEMS");
        builder.AppendLine(divider);

        foreach (var item in bill.Items)
        {
            foreach (var line in Wrap(item.ItemName, Width58))
                builder.AppendLine(line);

            var quantityAndRate =
                $"{item.Quantity} x {FormatMoney(item.Price, settings)}";
            AppendLeftRight(
                builder,
                quantityAndRate,
                FormatMoney(item.Total, settings),
                Width58);
        }

        builder.AppendLine(divider);
        AppendTotals58(builder, bill, settings);
        builder.AppendLine(divider);
        AppendPayments58(builder, bill, settings);
        builder.AppendLine(divider);
        AppendReceiptFooter(builder, settings, Width58);
        return builder.ToString();
    }

    private static void AppendReceiptHeader(
        StringBuilder builder,
        PrintBillDto bill,
        Settings? settings,
        int width)
    {
        var shopName = FirstNonBlank(settings?.ShopName, bill.HotelName, "SULTHAN ERP");
        AppendCenteredWrapped(builder, shopName, width);

        if (settings?.ShowShopAddressOnBill != false)
        {
            var address = FirstNonBlank(settings?.Address, bill.Address);
            if (!string.IsNullOrWhiteSpace(address))
                AppendCenteredWrapped(builder, address, width);
        }

        if (settings?.ShowShopPhoneOnBill == true &&
            !string.IsNullOrWhiteSpace(settings.Phone))
        {
            AppendCenteredWrapped(builder, $"Phone: {settings.Phone.Trim()}", width);
        }

        if (settings?.ShowGstNumberOnBill == true &&
            !string.IsNullOrWhiteSpace(settings.GstNumber))
        {
            AppendCenteredWrapped(builder, $"GST: {settings.GstNumber.Trim()}", width);
        }

        if (!string.IsNullOrWhiteSpace(settings?.HeaderMessage))
            AppendCenteredWrapped(builder, settings.HeaderMessage, width);
    }

    private static void AppendReceiptFooter(
        StringBuilder builder,
        Settings? settings,
        int width)
    {
        var footer = FirstNonBlank(
            settings?.FooterMessage,
            "Thank You - Visit Again");
        AppendCenteredWrapped(builder, footer, width);
    }

    private static void AppendTotals80(
        StringBuilder builder,
        PrintBillDto bill,
        Settings? settings)
    {
        AppendAmount(builder, "Subtotal", bill.SubTotal, Width80, settings);
        if (bill.Discount > 0)
            AppendAmount(builder, "Discount", -bill.Discount, Width80, settings);
        if (bill.Tax != 0 || settings?.ShowTaxOnCustomerBill == true)
            AppendAmount(builder, "Tax", bill.Tax, Width80, settings);
        AppendAmount(builder, "GRAND TOTAL", bill.GrandTotal, Width80, settings);
    }

    private static void AppendTotals58(
        StringBuilder builder,
        PrintBillDto bill,
        Settings? settings)
    {
        AppendAmount(builder, "SUBTOTAL", bill.SubTotal, Width58, settings);
        if (bill.Discount > 0)
            AppendAmount(builder, "DISCOUNT", -bill.Discount, Width58, settings);
        if (bill.Tax != 0 || settings?.ShowTaxOnCustomerBill == true)
            AppendAmount(builder, "TAX", bill.Tax, Width58, settings);
        AppendAmount(builder, "GRAND TOTAL", bill.GrandTotal, Width58, settings);
    }

    private static void AppendPayments80(
        StringBuilder builder,
        PrintBillDto bill,
        Settings? settings)
    {
        AppendPaymentSection(builder, bill, settings, Width80);
    }

    private static void AppendPayments58(
        StringBuilder builder,
        PrintBillDto bill,
        Settings? settings)
    {
        AppendPaymentSection(builder, bill, settings, Width58);
    }

    private static void AppendPaymentSection(
        StringBuilder builder,
        PrintBillDto bill,
        Settings? settings,
        int width)
    {
        if (IsPaymentPending(bill))
        {
            AppendCenteredWrapped(builder, "*** PAYMENT PENDING ***", width);
            var pendingAmount = bill.BalanceAmount > 0
                ? bill.BalanceAmount
                : bill.GrandTotal;
            AppendAmount(builder, "Balance", pendingAmount, width, settings);
            return;
        }

        if (bill.Payments.Count > 1)
            AppendCenteredWrapped(builder, "SPLIT PAYMENT", width);

        if (bill.Payments.Count == 0)
        {
            AppendAmount(
                builder,
                NormalizePaymentMethod(bill.PaymentMethod),
                bill.PaidAmount,
                width,
                settings);
        }
        else
        {
            foreach (var payment in bill.Payments)
            {
                var method = NormalizePaymentMethod(payment.PaymentMethod);
                AppendAmount(builder, method, payment.PaidAmount, width, settings);

                if (string.Equals(method, "CASH", StringComparison.Ordinal) &&
                    payment.TenderedAmount > payment.PaidAmount)
                {
                    AppendAmount(
                        builder,
                        "Tendered",
                        payment.TenderedAmount,
                        width,
                        settings);
                }
            }
        }

        var change = bill.Payments.Sum(x => x.ChangeAmount);
        if (change > 0)
            AppendAmount(builder, "Change", change, width, settings);
        AppendAmount(builder, "Balance", bill.BalanceAmount, width, settings);
    }

    private static void AppendAmount(
        StringBuilder builder,
        string label,
        decimal amount,
        int width,
        Settings? settings)
    {
        AppendLeftRight(
            builder,
            label,
            FormatMoney(amount, settings),
            width);
    }

    private static void AppendLabelValue(
        StringBuilder builder,
        string label,
        string? value,
        int width)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        var prefix = $"{label}: ";
        var firstWidth = Math.Max(1, width - prefix.Length);
        var lines = Wrap(value, firstWidth);
        builder.Append(prefix);
        builder.AppendLine(lines[0]);

        foreach (var continuation in lines.Skip(1))
        {
            foreach (var line in Wrap(continuation, width))
                builder.AppendLine(line);
        }
    }

    private static void AppendStaffDetails(
        StringBuilder builder,
        PrintBillDto bill,
        int width)
    {
        var hasCaptain = !string.IsNullOrWhiteSpace(bill.CaptainName);
        var hasCashier = !string.IsNullOrWhiteSpace(bill.CashierName);

        if (IsDineInOrderMode(bill.OrderType) && hasCaptain && hasCashier)
        {
            AppendLeftRight(
                builder,
                $"Captain: {bill.CaptainName}",
                $"Cashier: {bill.CashierName}",
                width);
            return;
        }

        if (IsDineInOrderMode(bill.OrderType) && hasCaptain)
            AppendLabelValue(builder, "Captain", bill.CaptainName, width);

        if (hasCashier)
            AppendLabelValue(builder, "Cashier", bill.CashierName, width);
    }

    private static void AppendLeftRight(
        StringBuilder builder,
        string left,
        string right,
        int width)
    {
        left = left.Trim();
        right = right.Trim();
        var spacing = width - left.Length - right.Length;
        if (spacing >= 1)
        {
            builder.Append(left);
            builder.Append(' ', spacing);
            builder.AppendLine(right);
            return;
        }

        foreach (var line in Wrap(left, width))
            builder.AppendLine(line);
        foreach (var line in Wrap(right, width))
            builder.AppendLine(PadLeft(line, width));
    }

    private static void AppendCenteredWrapped(
        StringBuilder builder,
        string? value,
        int width)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        foreach (var sourceLine in SplitLines(value))
        {
            foreach (var line in Wrap(sourceLine, width))
            {
                var padding = Math.Max(0, (width - line.Length) / 2);
                builder.Append(' ', padding);
                builder.AppendLine(line);
            }
        }
    }

    private static IEnumerable<string> SplitLines(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

    private static List<string> Wrap(string? value, int width)
    {
        var remaining = string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
        var lines = new List<string>();
        while (remaining.Length > width)
        {
            var splitAt = remaining.LastIndexOf(' ', width - 1, width);
            if (splitAt <= 0)
                splitAt = width;
            lines.Add(remaining[..splitAt].TrimEnd());
            remaining = remaining[splitAt..].TrimStart();
        }

        lines.Add(remaining);
        return lines;
    }

    private static string FormatMoney(decimal amount, Settings? settings)
    {
        var decimalPlaces = Math.Clamp(settings?.DecimalPlaces ?? 2, 0, 4);
        return amount.ToString($"F{decimalPlaces}");
    }

    private static string NormalizePaymentMethod(string? method) =>
        method?.Trim().ToUpperInvariant() switch
        {
            "UPI" => "UPI",
            "CARD" => "CARD",
            "CASH" => "CASH",
            "MIXED" => "SPLIT PAYMENT",
            { Length: > 0 } value => value,
            _ => "PAYMENT"
        };

    private static string NormalizeOrderMode(string? orderType) =>
        orderType?.Trim().ToUpperInvariant() switch
        {
            "DINEIN" or "DINE IN" => "DINE IN",
            "PARCEL" or "TAKE AWAY" => "TAKE AWAY",
            "HOMEDELIVERY" or "HOME DELIVERY" => "HOME DELIVERY",
            "PHONEORDER" or "PHONE ORDER" => "PHONE ORDER",
            { Length: > 0 } value => value,
            _ => "TAKE AWAY"
        };

    private static bool IsDineInOrderMode(string? orderType) =>
        string.Equals(
            NormalizeOrderMode(orderType),
            "DINE IN",
            StringComparison.Ordinal);

    private static bool IsPaymentPending(PrintBillDto bill) =>
        string.Equals(
            bill.PaymentMethod,
            "PAYMENT PENDING",
            StringComparison.OrdinalIgnoreCase);

    private static bool Is58mm(string? width) =>
        string.Equals(width?.Trim(), "58MM", StringComparison.OrdinalIgnoreCase);

    private static string CompactBillNumber(string? billNumber)
    {
        if (string.IsNullOrWhiteSpace(billNumber))
            return "-";

        return billNumber.Split(
                '-',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .LastOrDefault() ?? billNumber.Trim();
    }

    private static string FirstNonBlank(params string?[] values) =>
        values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim() ?? string.Empty;

    private static string PadRight(string value, int width) =>
        value.PadRight(width);

    private static string PadLeft(string value, int width) =>
        value.PadLeft(width);
}
