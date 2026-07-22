using Sulthan.Core.DTOs.Orders;
using Sulthan.Core.Entities;
using Sulthan.Core.Interfaces;

namespace Sulthan.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;

    public OrderService(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<IEnumerable<Order>> GetAllAsync()
    {
        return await _orderRepository.GetAllAsync();
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        return await _orderRepository.GetByIdAsync(id);
    }

    public async Task<Order> AddAsync(CreateOrderDto dto)
    {
        var order = new Order
        {
            OrderType = dto.OrderType,
            DiningTableId = dto.DiningTableId ?? 0,
            CustomerId = dto.CustomerId,
            UserId = dto.UserId,

            // Temporary values
            BillNumber = string.Empty,
            BillStatus = Sulthan.Core.Enums.OrderStatus.Pending,
            SubTotal = 0,
            Discount = 0,
            Tax = 0,
            GrandTotal = 0
        };

        foreach (var item in dto.Items)
        {
            order.Items.Add(new OrderItem
            {
                MenuItemId = item.MenuItemId,
                Quantity = item.Quantity,
                Notes = item.Notes,
                Price = 0 // We'll calculate this in the next step
            });
        }

        return await _orderRepository.AddAsync(order);
    }

    public async Task<Order> UpdateAsync(int id, UpdateOrderDto dto)
    {
        var order = await _orderRepository.GetByIdAsync(id);

        if (order == null)
            throw new Exception("Order not found.");

        order.BillStatus = dto.OrderStatus;
        order.DiningTableId = dto.DiningTableId ?? order.DiningTableId;
        order.CustomerId = dto.CustomerId;

        return await _orderRepository.UpdateAsync(order);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _orderRepository.DeleteAsync(id);
    }
}