using Sulthan.Core.Entities;

namespace Sulthan.Core.Interfaces;

public interface IKitchenOrderTicketRepository
{
    Task<KitchenOrderTicket?> GetByIdAsync(int id);

    Task<List<KitchenOrderTicket>> GetAllAsync();

    Task<KitchenOrderTicket> AddAsync(KitchenOrderTicket ticket);
}