using Sulthan.Core.DTOs.Orders;
using Sulthan.Core.Entities;

namespace Sulthan.Core.Interfaces;

public interface IOrderService
{
    Task<IEnumerable<Order>> GetAllAsync();

    Task<Order?> GetByIdAsync(int id);

    Task<Order> AddAsync(CreateOrderDto dto);

    Task<Order> UpdateAsync(int id, UpdateOrderDto dto);

    Task<bool> DeleteAsync(int id);
}