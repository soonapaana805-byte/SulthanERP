using Sulthan.Core.DTOs.Orders.Response;
using Sulthan.Core.DTOs.Payments;
using Sulthan.Core.DTOs.Payments.Response;
using Sulthan.Core.Entities;
using Sulthan.Core.Enums;
using Sulthan.Core.Interfaces;

namespace Sulthan.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IOrderRepository _orderRepository;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IOrderRepository orderRepository)
    {
        _paymentRepository = paymentRepository;
        _orderRepository = orderRepository;
    }

    public async Task<IEnumerable<PaymentResponseDto>> GetAllAsync()
    {
        var payments = await _paymentRepository.GetAllAsync();

        return payments.Select(MapToDto).ToList();
    }

    public async Task<PaymentResponseDto?> GetByIdAsync(int id)
    {
        var payment = await _paymentRepository.GetByIdAsync(id);

        if (payment == null)
            return null;

        return MapToDto(payment);
    }

    public async Task<PaymentResponseDto?> GetByOrderIdAsync(int orderId)
    {
        var payment = await _paymentRepository.GetByOrderIdAsync(orderId);

        if (payment == null)
            return null;

        return MapToDto(payment);
    }

    public async Task<PaymentResponseDto> AddAsync(CreatePaymentDto dto)
    {
        var order = await _orderRepository.GetByIdAsync(dto.OrderId);

        if (order == null)
            throw new Exception("Order not found.");

        var existingPayment = await _paymentRepository.GetByOrderIdAsync(dto.OrderId);

        if (existingPayment != null)
            throw new Exception("Payment already exists for this order.");

        var payment = new Payment
        {
            OrderId = order.Id,
            BillAmount = order.SubTotal,
            DiscountAmount = order.Discount,
            TaxAmount = order.Tax,
            GrandTotal = order.GrandTotal,
            PaymentMethod = dto.PaymentMethod,
            PaidAmount = dto.PaidAmount,
            BalanceAmount = order.GrandTotal - dto.PaidAmount,
            TransactionNumber = dto.TransactionNumber,
            PaymentDate = DateTime.Now,
            PaymentStatus = dto.PaidAmount >= order.GrandTotal
                ? PaymentStatus.Paid
                : PaymentStatus.Pending,
            UserId = order.UserId
        };

        var savedPayment = await _paymentRepository.AddAsync(payment);

        return MapToDto(savedPayment);
    }

    public async Task<PaymentResponseDto> UpdateAsync(int id, UpdatePaymentDto dto)
    {
        var payment = await _paymentRepository.GetByIdAsync(id);

        if (payment == null)
            throw new Exception("Payment not found.");

        payment.PaymentMethod = dto.PaymentMethod;
        payment.PaidAmount = dto.PaidAmount;
        payment.BalanceAmount = payment.GrandTotal - dto.PaidAmount;
        payment.TransactionNumber = dto.TransactionNumber;
        payment.PaymentStatus = dto.PaymentStatus;
        payment.PaymentDate = DateTime.Now;

        var updatedPayment = await _paymentRepository.UpdateAsync(payment);

        return MapToDto(updatedPayment);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _paymentRepository.DeleteAsync(id);
    }

    public async Task<PaymentSummaryDto> GetSummaryAsync()
    {
        var payments = await _paymentRepository.GetAllAsync();

        return new PaymentSummaryDto
        {
            TotalPayments = payments.Count(),
            TotalCash = payments.Where(x => x.PaymentMethod == PaymentMode.Cash).Sum(x => x.PaidAmount),
            TotalCard = payments.Where(x => x.PaymentMethod == PaymentMode.Card).Sum(x => x.PaidAmount),
            TotalUpi = payments.Where(x => x.PaymentMethod == PaymentMode.Upi).Sum(x => x.PaidAmount),
            TotalMixed = payments.Where(x => x.PaymentMethod == PaymentMode.Mixed).Sum(x => x.PaidAmount),
            GrandTotal = payments.Sum(x => x.PaidAmount)
        };
    }

    private static PaymentResponseDto MapToDto(Payment payment)
    {
        return new PaymentResponseDto
        {
            Id = payment.Id,

            Order = payment.Order == null
                ? null
                : new OrderSummaryDto
                {
                    Id = payment.Order.Id,
                    BillNumber = payment.Order.BillNumber
                },

            Cashier = payment.User == null
                ? null
                : new UserSummaryDto
                {
                    Id = payment.User.Id,
                    FullName = payment.User.FullName
                },

            BillAmount = payment.BillAmount,
            DiscountAmount = payment.DiscountAmount,
            TaxAmount = payment.TaxAmount,
            GrandTotal = payment.GrandTotal,
            PaymentMethod = payment.PaymentMethod,
            PaymentStatus = payment.PaymentStatus,
            PaidAmount = payment.PaidAmount,
            BalanceAmount = payment.BalanceAmount,
            TransactionNumber = payment.TransactionNumber,
            PaymentDate = payment.PaymentDate
        };
    }
}