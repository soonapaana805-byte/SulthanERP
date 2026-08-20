using System.Text;
using Microsoft.Extensions.Options;
using Sulthan.Core.Entities;
using Sulthan.Core.Enums;

namespace SulthanERP.Api.Printing;

public sealed class KitchenKotCancellationFormatter
{
    private readonly IOptionsMonitor<KitchenPrintingOptions> _options;

    public KitchenKotCancellationFormatter(
        IOptionsMonitor<KitchenPrintingOptions> options)
    {
        _options = options;
    }

    public string Format(KitchenPrintJob job)
    {
        var ticket = job.KitchenOrderTicket
            ?? throw new InvalidOperationException("The print job has no KOT.");
        var order = ticket.Order
            ?? throw new InvalidOperationException("The KOT has no order.");
        var audit = job.KotCancellationAudit
            ?? throw new InvalidOperationException(
                "The cancellation print job has no audit record.");
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
        AppendCentered(builder, "*** KOT CANCELLED ***", width);
        AppendLeftRight(
            builder,
            $"KOT: {CompactNumber(ticket.KotNumber, "KOT-")}",
            $"BILL: {CompactNumber(order.BillNumber, string.Empty)}",
            width);
        AppendLeftRight(
            builder,
            order.OrderType == OrderType.DineIn
                ? $"TABLE: {order.DiningTable?.TableNumber ?? "DINE IN"}"
                : $"MODE: {FormatMode(order.OrderType)}",
            $"TIME: {audit.CancelledOn.ToLocalTime():HH:mm:ss}",
            width);
        builder.AppendLine(divider);

        var items = audit.Items
            .Where(x => string.Equals(
                ResolveKitchenName(x.KitchenName),
                ResolveKitchenName(job.KitchenName),
                StringComparison.OrdinalIgnoreCase))
            .ToList();
        foreach (var item in items)
        {
            AppendWrapped(
                builder,
                $"{item.CancelledQuantity:0.##} x {item.ItemName}",
                width);
        }

        builder.AppendLine(divider);
        AppendWrapped(builder, $"REASON: {audit.Reason}", width);
        AppendWrapped(builder, $"REQUESTED BY: {audit.RequestedByName}", width);
        AppendWrapped(builder, $"APPROVED BY: {audit.ApprovedByName}", width);
        AppendWrapped(
            builder,
            $"CANCELLED: {audit.CancelledOn.ToLocalTime():dd-MM-yyyy HH:mm}",
            width);
        return builder.ToString();
    }

    private static string FormatMode(OrderType orderType) => orderType switch
    {
        OrderType.Parcel => "TAKE AWAY",
        OrderType.HomeDelivery => "HOME DELIVERY",
        _ => orderType.ToString().ToUpperInvariant()
    };

    private static string ResolveKitchenName(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "Main Kitchen" : value.Trim();

    private static string CompactNumber(string value, string prefix)
    {
        var compact = value.Split(
            '-',
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries).LastOrDefault() ?? value;
        return string.IsNullOrEmpty(prefix) ? compact : $"{prefix}{compact}";
    }

    private static void AppendCentered(
        StringBuilder builder,
        string value,
        int width)
    {
        var text = value.Length > width ? value[..width] : value;
        builder.Append(' ', Math.Max(0, (width - text.Length) / 2));
        builder.AppendLine(text);
    }

    private static void AppendLeftRight(
        StringBuilder builder,
        string left,
        string right,
        int width)
    {
        var spaces = width - left.Length - right.Length;
        if (spaces > 0)
        {
            builder.Append(left);
            builder.Append(' ', spaces);
            builder.AppendLine(right);
            return;
        }

        AppendWrapped(builder, left, width);
        AppendWrapped(builder, right, width);
    }

    private static void AppendWrapped(
        StringBuilder builder,
        string value,
        int width)
    {
        var remaining = value.Trim();
        while (remaining.Length > 0)
        {
            var take = Math.Min(width, remaining.Length);
            if (take < remaining.Length)
            {
                var lastSpace = remaining.LastIndexOf(' ', take - 1, take);
                if (lastSpace > 0)
                    take = lastSpace;
            }

            builder.AppendLine(remaining[..take].TrimEnd());
            remaining = remaining[take..].TrimStart();
        }
    }
}
