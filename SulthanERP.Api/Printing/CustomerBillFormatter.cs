using Sulthan.Core.Common;
using Sulthan.Core.DTOs.Billing;
using Sulthan.Core.Entities;
using Sulthan.Core.Interfaces;

namespace SulthanERP.Api.Printing;

public sealed class CustomerBillFormatter
{
    private readonly IReceiptFormatter _receiptFormatter;

    public CustomerBillFormatter(IReceiptFormatter receiptFormatter)
    {
        _receiptFormatter = receiptFormatter;
    }

    public Task<string> FormatAsync(
        CustomerBillPrintJob job,
        Payment? payment)
    {
        var order = job.Order
            ?? throw new InvalidOperationException("The print job has no order.");
        var isPendingBill = string.Equals(
            job.DocumentType,
            CustomerBillDocumentType.PendingBill,
            StringComparison.Ordinal);

        if (!isPendingBill && payment is null)
            throw new InvalidOperationException("The paid receipt has no payment.");

        var paymentLines = payment?.Allocations.Count > 0
            ? payment.Allocations
                .OrderBy(x => x.Id)
                .Select(x => new BillPaymentDto
                {
                    PaymentMethod = x.PaymentMethod.ToString(),
                    PaidAmount = x.Amount,
                    TenderedAmount = x.TenderedAmount,
                    ChangeAmount = x.ChangeAmount,
                    TransactionNumber = x.TransactionNumber,
                    PaymentDate = payment.PaymentDate
                })
                .ToList()
            : [];

        return _receiptFormatter.GenerateReceiptAsync(
            new PrintBillDto
            {
                DocumentTitle = isPendingBill
                    ? "CUSTOMER BILL"
                    : "CUSTOMER RECEIPT",
                IsReprint = job.IsReprint,
                BillNumber = order.BillNumber,
                BillDate = isPendingBill
                    ? (order.BillRequestedOn ?? DateTime.UtcNow).ToLocalTime()
                    : payment!.PaymentDate,
                TableNumber = order.DiningTable?.TableNumber ?? string.Empty,
                CustomerName = order.CustomerName ?? order.Customer?.Name,
                CaptainName = order.User?.FullName ?? string.Empty,
                CashierName = job.RequestedByUser?.FullName ??
                    payment?.User?.FullName ??
                    "Unknown",
                OrderType = ResolveReceiptOrderMode(order),
                SubTotal = order.SubTotal,
                Discount = order.Discount,
                Tax = order.Tax,
                GrandTotal = order.GrandTotal,
                PaidAmount = isPendingBill ? 0m : payment!.PaidAmount,
                BalanceAmount = isPendingBill
                    ? order.GrandTotal
                    : payment!.BalanceAmount,
                PaymentMethod = isPendingBill
                    ? "PAYMENT PENDING"
                    : payment!.PaymentMethod.ToString(),
                Payments = paymentLines,
                Items = order.Items
                    .Where(x => x.Quantity > x.CancelledQuantity)
                    .Select(x => new BillItemDto
                {
                    MenuItemId = x.MenuItemId,
                    ItemName = x.MenuItem?.Name ?? $"Item #{x.MenuItemId}",
                    Price = x.Price,
                    Quantity = x.Quantity - x.CancelledQuantity,
                    Total = x.Price * (x.Quantity - x.CancelledQuantity)
                }).ToList()
            });
    }

    private static string ResolveReceiptOrderMode(Order order)
    {
        if (order.OrderType == Sulthan.Core.Enums.OrderType.DineIn)
            return "DINE IN";

        if (order.OrderType == Sulthan.Core.Enums.OrderType.HomeDelivery)
            return "HOME DELIVERY";

        if (order.OrderType == Sulthan.Core.Enums.OrderType.Parcel &&
            string.Equals(
                order.Remarks?.Trim(),
                "Phone order",
                StringComparison.OrdinalIgnoreCase))
        {
            return "PHONE ORDER";
        }

        return "TAKE AWAY";
    }
}
