using Sulthan.Core.DTOs.Payments;

namespace Sulthan.Core.Interfaces;

public interface IPaymentService
{
    Task<IEnumerable<PaymentResponseDto>> GetAllAsync();

    Task<PaymentResponseDto?> GetByIdAsync(int id);

    Task<PaymentResponseDto?> GetByOrderIdAsync(int orderId);

    Task<PaymentResponseDto> AddAsync(CreatePaymentDto dto);

    Task<PaymentResponseDto> UpdateAsync(int id, UpdatePaymentDto dto);

    Task<bool> DeleteAsync(int id);

    Task<PaymentSummaryDto> GetSummaryAsync();
}