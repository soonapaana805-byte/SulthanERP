using Sulthan.Core.Entities;

namespace Sulthan.Core.Interfaces;

public interface IPaymentRepository
{
    Task<IEnumerable<Payment>> GetAllAsync();

    Task<Payment?> GetByIdAsync(int id);

    Task<Payment?> GetByOrderIdAsync(int orderId);

    Task<Payment> AddAsync(Payment payment);

    Task<Payment> UpdateAsync(Payment payment);

    Task<bool> DeleteAsync(int id);
}