using Sulthan.Core.DTOs.KitchenOrders;
using Sulthan.Core.Entities;

namespace Sulthan.Core.Interfaces;

public interface IKitchenOrderTicketService
{
    Task<List<KitchenOrderTicket>> GetAllAsync();

    Task<KitchenOrderTicket?> GetByIdAsync(int id);

    Task<KitchenOrderTicket> CreateAsync(CreateKitchenOrderTicketDto dto);
}