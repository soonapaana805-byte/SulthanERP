using Sulthan.Core.DTOs.KitchenOrders;
using Sulthan.Core.Entities;
using Sulthan.Core.Interfaces;

namespace Sulthan.Infrastructure.Services;

public class KitchenOrderTicketService : IKitchenOrderTicketService
{
    private readonly IKitchenOrderTicketRepository _repository;

    public KitchenOrderTicketService(IKitchenOrderTicketRepository repository)
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

    public async Task<KitchenOrderTicket> CreateAsync(CreateKitchenOrderTicketDto dto)
    {
        var ticket = new KitchenOrderTicket
        {
            KotNumber = $"KOT{DateTime.Now:yyyyMMddHHmmss}",
            OrderId = dto.OrderId,
            PrintedOn = DateTime.Now,
            IsReprint = false
        };

        foreach (var item in dto.Items)
        {
            ticket.Items.Add(new KitchenOrderTicketItem
            {
                MenuItemId = item.MenuItemId,
                Quantity = item.Quantity,
                Notes = item.Notes
            });
        }

        return await _repository.AddAsync(ticket);
    }
}