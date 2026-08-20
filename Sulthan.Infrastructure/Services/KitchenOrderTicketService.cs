using Sulthan.Core.DTOs.KitchenOrders;
using Sulthan.Core.Entities;
using Sulthan.Core.Interfaces;

namespace Sulthan.Infrastructure.Services;

public class KitchenOrderTicketService
    : IKitchenOrderTicketService
{
    private readonly IKitchenOrderTicketRepository _repository;

    public KitchenOrderTicketService(
        IKitchenOrderTicketRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<KitchenOrderTicket>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<KitchenOrderTicket?> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<KitchenOrderTicket> CreateAsync(
        CreateKitchenOrderTicketDto dto)
    {
        if (dto.OrderId <= 0)
        {
            throw new ArgumentException(
                "Valid order ID is required.");
        }

        if (dto.Items == null || dto.Items.Count == 0)
        {
            throw new ArgumentException(
                "At least one kitchen order item is required.");
        }

        var ticket = new KitchenOrderTicket
        {
            KotNumber =
                $"KOT{DateTime.Now:yyyyMMddHHmmss}",
            OrderId = dto.OrderId,
            PrintedOn = DateTime.Now,
            IsReprint = false
        };

        foreach (var item in dto.Items)
        {
            if (item.MenuItemId <= 0)
            {
                throw new ArgumentException(
                    "Valid menu item ID is required.");
            }

            if (item.Quantity <= 0)
            {
                throw new ArgumentException(
                    "Kitchen item quantity must be " +
                    "greater than zero.");
            }


            if (!item.OrderItemId.HasValue)
            {
                throw new ArgumentException(
                    "OrderItemId is required for every new KOT item.");
            }

            var ownedOrderItem = await _repository.GetOrderItemAsync(
                item.OrderItemId.Value,
                dto.OrderId);
            if (ownedOrderItem is null)
            {
                throw new ArgumentException(
                    "The supplied order item does not belong to this order.");
            }

            if (ownedOrderItem.MenuItemId != item.MenuItemId ||
                ownedOrderItem.Quantity != item.Quantity)
            {
                throw new ArgumentException(
                    "The KOT item must exactly match its owned order item.");
            }

            ticket.Items.Add(
                new KitchenOrderTicketItem
                {
                    OrderItemId = item.OrderItemId,
                    MenuItemId = item.MenuItemId,
                    Quantity = item.Quantity,
                    Notes = item.Notes
                });
        }

        return await _repository.AddAsync(ticket);
    }
}
