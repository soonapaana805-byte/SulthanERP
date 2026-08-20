using System.Text;
using Microsoft.Extensions.Options;
using Sulthan.Core.Entities;
using Sulthan.Core.Enums;

namespace SulthanERP.Api.Printing;

public sealed class KitchenKotFormatter
{
    private readonly IOptionsMonitor<KitchenPrintingOptions> _options;

    public KitchenKotFormatter(IOptionsMonitor<KitchenPrintingOptions> options)
    {
        _options = options;
    }

    public string Format(KitchenPrintJob job)
    {
        var ticket = job.KitchenOrderTicket
            ?? throw new InvalidOperationException("The print job has no KOT.");
        var order = ticket.Order
            ?? throw new InvalidOperationException("The KOT has no order.");
        var width = Math.Clamp(
            _options.CurrentValue.PaperWidthCharacters,
            32,
            64);
        var divider = new string('-', width);
        var builder = new StringBuilder();

        AppendLeftRight(
            builder,
            "SULTHAN ERP",
            ResolveKitchenName(job.KitchenName).ToUpperInvariant(),
            width);
        builder.AppendLine(divider);
        if (ticket.IsReprint)
            AppendCentered(builder, "*** REPRINT ***", width);
        AppendLeftRight(
            builder,
            $"KOT NO: {CompactKotNumber(ticket.KotNumber, order.BillNumber)}",
            $"TIME: {ticket.CreatedOn.ToLocalTime():HH:mm:ss}",
            width);
        AppendLeftRight(
            builder,
            order.OrderType == OrderType.DineIn
                ? $"TABLE: {order.DiningTable?.TableNumber ?? "DINE IN"}"
                : "MODE: TAKE AWAY",
            $"CAP: {order.User?.FullName ?? "Unknown"}",
            width);
        builder.AppendLine(divider);

        var kitchenItems = ticket.Items
            .Where(item => string.Equals(
                ResolveKitchenName(item.MenuItem?.KitchenName),
                job.KitchenName,
                StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var item in kitchenItems)
        {
            var quantity = item.Quantity.ToString("0.##");
            AppendWrapped(
                builder,
                $"{quantity} x {item.MenuItem?.Name ?? $"Item #{item.MenuItemId}"}",
                width,
                string.Empty);

            if (!string.IsNullOrWhiteSpace(item.Notes))
            {
                AppendWrapped(
                    builder,
                    $"NOTE: {item.Notes.Trim()}",
                    width,
                    "      ");
            }
        }

        builder.AppendLine(divider);
        builder.AppendLine($"ITEMS : {kitchenItems.Sum(item => item.Quantity):0.##}");
        AppendCentered(builder, "END OF KOT", width);
        return builder.ToString();
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

        AppendWrapped(builder, left, width, string.Empty);
        AppendWrapped(builder, right, width, string.Empty);
    }

    private static void AppendCentered(
        StringBuilder builder,
        string value,
        int width)
    {
        var trimmed = value.Length > width ? value[..width] : value;
        var leftPadding = Math.Max(0, (width - trimmed.Length) / 2);
        builder.Append(' ', leftPadding);
        builder.AppendLine(trimmed);
    }

    private static void AppendWrapped(
        StringBuilder builder,
        string value,
        int width,
        string continuationPrefix)
    {
        var remaining = value.Trim();
        var firstLine = true;
        while (remaining.Length > 0)
        {
            var prefix = firstLine ? string.Empty : continuationPrefix;
            var availableWidth = Math.Max(1, width - prefix.Length);
            var take = Math.Min(availableWidth, remaining.Length);

            if (take < remaining.Length)
            {
                var lastSpace = remaining.LastIndexOf(' ', take - 1, take);
                if (lastSpace > 0)
                    take = lastSpace;
            }

            builder.Append(prefix);
            builder.AppendLine(remaining[..take].TrimEnd());
            remaining = remaining[take..].TrimStart();
            firstLine = false;
        }
    }

    private static string ResolveKitchenName(string? kitchenName)
    {
        return string.IsNullOrWhiteSpace(kitchenName)
            ? "Main Kitchen"
            : kitchenName.Trim();
    }

    private static string CompactKotNumber(
        string kotNumber,
        string billNumber)
    {
        var compactBillNumber = billNumber
            .Split(
                '-',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .LastOrDefault() ?? billNumber;
        var firstKotNumber = $"KOT-{billNumber}";

        if (string.Equals(
                kotNumber,
                firstKotNumber,
                StringComparison.OrdinalIgnoreCase))
        {
            return compactBillNumber;
        }

        var additionalKotPrefix = firstKotNumber + "-";
        if (kotNumber.StartsWith(
                additionalKotPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            return $"{compactBillNumber}-{kotNumber[additionalKotPrefix.Length..]}";
        }

        return kotNumber.StartsWith("KOT-", StringComparison.OrdinalIgnoreCase)
            ? kotNumber[4..]
            : kotNumber;
    }
}
