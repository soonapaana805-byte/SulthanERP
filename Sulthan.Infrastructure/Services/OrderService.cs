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
        var billNumber =
            await _billCounterRepository.GetNextBillNumberAsync();

        DiningTable? table = null;

        if (dto.OrderType == OrderType.DineIn)
        {
            if (!dto.DiningTableId.HasValue)
            {
                throw new ArgumentException(
                    "Dining table is required.");
            }

            table = await _tableRepository.GetByIdAsync(
                dto.DiningTableId.Value);

            if (table == null)
            {
                throw new KeyNotFoundException(
                    "Dining table not found.");
            }

            if (!string.Equals(
                    table.Status,
                    "Available",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Table {table.TableNumber} is not available.");
            }

            table.Status = "PaymentPending";

            await _tableRepository.UpdateAsync(table);
        }

        var order = new Order
        {
            BillNumber = billNumber,
            OrderType = dto.OrderType,
            BillStatus = OrderStatus.Pending,
            DiningTableId = dto.OrderType == OrderType.DineIn
                ? dto.DiningTableId
                : null,
            CustomerId = dto.CustomerId,
            UserId = dto.UserId,
            Discount = 0,
            Tax = 0
        };

        decimal subTotal = 0;

        foreach (var item in dto.Items)
        {
            var menuItem =
                await _menuItemRepository.GetByIdAsync(
                    item.MenuItemId);

            if (menuItem == null)
            {
                throw new KeyNotFoundException(
                    $"Menu item {item.MenuItemId} not found.");
            }

            decimal price;

            if (dto.OrderType == OrderType.Parcel ||
                dto.OrderType == OrderType.HomeDelivery)
            {
                price = menuItem.ParcelPrice;
            }
            else if (string.Equals(
                         table!.TableType,
                         "AC",
                         StringComparison.OrdinalIgnoreCase))
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
                Price = price,
                Notes = item.Notes
            });

            subTotal += price * item.Quantity;
        }

        order.SubTotal = subTotal;
        order.GrandTotal = subTotal;

        return await _orderRepository.AddAsync(order);
    }

    public async Task<Order> UpdateAsync(
        int id,
        UpdateOrderDto dto)
    {
        var order =
            await _orderRepository.GetByIdAsync(id);

        if (order == null)
        {
            throw new KeyNotFoundException(
                "Order not found.");
        }

        order.BillStatus = dto.OrderStatus;

        if (order.OrderType == OrderType.DineIn)
        {
            order.DiningTableId =
                dto.DiningTableId ?? order.DiningTableId;
        }
        else
        {
            order.DiningTableId = null;
        }

        order.CustomerId = dto.CustomerId;

        return await _orderRepository.UpdateAsync(order);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _orderRepository.DeleteAsync(id);
    }

    public async Task<Order> CompleteOrderAsync(
        int id,
        CompleteOrderDto dto)
    {
        var order =
            await _orderRepository.GetByIdAsync(id);

        if (order == null)
        {
            throw new KeyNotFoundException(
                "Order not found.");
        }

        order.Discount = dto.Discount;
        order.Tax = dto.Tax;
        order.GrandTotal =
            order.SubTotal - order.Discount + order.Tax;
        order.BillStatus = OrderStatus.Paid;

        await _orderRepository.UpdateAsync(order);

        if (order.OrderType == OrderType.DineIn &&
            order.DiningTableId.HasValue)
        {
            var table =
                await _tableRepository.GetByIdAsync(
                    order.DiningTableId.Value);

            if (table != null)
            {
                table.Status = "CleaningPending";

                await _tableRepository.UpdateAsync(table);
            }
        }

        return order;
    }
}