using Sulthan.Core.DTOs.Orders;
using Sulthan.Core.Entities;
using Sulthan.Core.Enums;
using Sulthan.Core.Interfaces;

namespace Sulthan.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IBillCounterRepository _billCounterRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ITableRepository _tableRepository;

    public OrderService(
        IOrderRepository orderRepository,
        IBillCounterRepository billCounterRepository,
        IMenuItemRepository menuItemRepository,
        ITableRepository tableRepository)
    {
        _orderRepository = orderRepository;
        _billCounterRepository = billCounterRepository;
        _menuItemRepository = menuItemRepository;
        _tableRepository = tableRepository;
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
        var billNumber = await _billCounterRepository.GetNextBillNumberAsync();

        DiningTable? table = null;

        if (dto.OrderType != OrderType.Parcel && dto.DiningTableId.HasValue)
        {
            table = await _tableRepository.GetByIdAsync(dto.DiningTableId.Value);

            if (table == null)
                throw new Exception("Dining table not found.");
        }

        var order = new Order
        {
            BillNumber = billNumber,
            OrderType = dto.OrderType,
            BillStatus = OrderStatus.Pending,
            DiningTableId = dto.DiningTableId ?? 0,
            CustomerId = dto.CustomerId,
            UserId = dto.UserId,
            Discount = 0,
            Tax = 0
        };

        decimal subTotal = 0;

        foreach (var item in dto.Items)
        {
            var menuItem = await _menuItemRepository.GetByIdAsync(item.MenuItemId);

            if (menuItem == null)
                throw new Exception($"Menu item {item.MenuItemId} not found.");

            decimal price;

            if (dto.OrderType == OrderType.Parcel)
            {
                price = menuItem.ParcelPrice;
            }
            else if (table!.TableType == "AC")
            {
                price = menuItem.ACPrice;
            }
            else
            {
                price = menuItem.NonACPrice;
            }

            order.Items.Add(new OrderItem
            {
                MenuItemId = item.MenuItemId,
                Quantity = item.Quantity,
                Notes = item.Notes,
                Price = price
            });

            subTotal += price * item.Quantity;
        }

        order.SubTotal = subTotal;
        order.GrandTotal = subTotal - order.Discount + order.Tax;

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