using Sulthan.Core.DTOs.Billing;
using Sulthan.Core.Interfaces;

namespace Sulthan.Infrastructure.Services;

public class BillingService : IBillingService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IReceiptFormatter _receiptFormatter;

    public BillingService(
       IOrderRepository orderRepository,
       IPaymentRepository paymentRepository,
       IReceiptFormatter receiptFormatter)
    {
        _orderRepository = orderRepository;
        _paymentRepository = paymentRepository;
        _receiptFormatter = receiptFormatter;
    }

    public async Task<BillResponseDto?> GetBillAsync(int orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);

        if (order == null)
            return null;

        var payment = await _paymentRepository.GetByOrderIdAsync(orderId);

        if (payment == null)
            return null;

        var bill = new BillResponseDto
        {
            BillNumber = order.BillNumber,
            OrderType = order.OrderType.ToString(),
            TableNumber = order.DiningTable?.TableNumber,
            CustomerName = order.Customer?.Name,
            CaptainName = order.User?.FullName ?? string.Empty,
            BillDate = payment.PaymentDate,

            SubTotal = order.SubTotal,
            Discount = order.Discount,
            Tax = order.Tax,
            GrandTotal = order.GrandTotal,

            PaidAmount = payment.PaidAmount,
            BalanceAmount = payment.BalanceAmount,
            PaymentMethod = payment.PaymentMethod.ToString()
        };

        foreach (var item in order.Items)
        {
            bill.Items.Add(new BillItemDto
            {
                MenuItemId = item.MenuItemId,
                ItemName = item.MenuItem?.Name ?? string.Empty,
                Price = item.Price,
                Quantity = item.Quantity,
                Total = item.Price * item.Quantity
            });
        }

        return bill;
    }

    public async Task<BillResponseDto?> ReprintBillAsync(string billNumber)
    {
        var orders = await _orderRepository.GetAllAsync();

        var order = orders.FirstOrDefault(x => x.BillNumber == billNumber);

        if (order == null)
            return null;

        return await GetBillAsync(order.Id);
    }
    public async Task<string?> PrintBillAsync(string billNumber)
    {
        var bill = await ReprintBillAsync(billNumber);

        if (bill == null)
            return null;

        return await _receiptFormatter.Generate80mmReceiptAsync(
            new PrintBillDto
            {
                BillNumber = bill.BillNumber,
                TableNumber = bill.TableNumber,
                CustomerName = bill.CustomerName,
                CaptainName = bill.CaptainName,
                CashierName = "Admin",
                BillDate = bill.BillDate,
                SubTotal = bill.SubTotal,
                Discount = bill.Discount,
                Tax = bill.Tax,
                GrandTotal = bill.GrandTotal,
                PaidAmount = bill.PaidAmount,
                BalanceAmount = bill.BalanceAmount,
                PaymentMethod = bill.PaymentMethod,
                Items = bill.Items
            });
    }
}