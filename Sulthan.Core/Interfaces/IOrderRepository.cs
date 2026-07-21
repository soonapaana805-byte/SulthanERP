using Sulthan.Core.Entities;

namespace Sulthan.Core.Interfaces
{
    public interface IOrderRepository
    {
        Task<IEnumerable<Order>> GetAllAsync();

        Task<Order?> GetByIdAsync(int id);

        Task<Order> AddAsync(Order order);

        Task<Order> UpdateAsync(Order order);

        Task<bool> DeleteAsync(int id);
    }
}