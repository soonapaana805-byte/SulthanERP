using Microsoft.EntityFrameworkCore;
using Sulthan.Core.Entities;
using Sulthan.Core.Interfaces;
using Sulthan.Infrastructure.Data;

namespace Sulthan.Infrastructure.Repositories;

public class KitchenOrderTicketRepository : IKitchenOrderTicketRepository
{
    private readonly RestaurantDbContext _context;

    public KitchenOrderTicketRepository(RestaurantDbContext context)
    {
        _context = context;
    }

    public async Task<KitchenOrderTicket?> GetByIdAsync(int id)
    {
        return await _context.KitchenOrderTickets
            .Include(x => x.Order!)
            .ThenInclude(x => x.Items)
            .Include(x => x.Items)
            .ThenInclude(x => x.MenuItem)
            .Include(x => x.Items)
            .ThenInclude(x => x.OrderItem)
            .FirstOrDefaultAsync(x => x.Id == id);
    }


    public async Task<List<KitchenOrderTicket>> GetAllAsync()
    {
        return await _context.KitchenOrderTickets
            .Include(x => x.Order)
            .Include(x => x.Items)
            .ThenInclude(x => x.MenuItem)
            .Include(x => x.Items)
            .ThenInclude(x => x.OrderItem)
            .OrderByDescending(x => x.CreatedOn)
            .ToListAsync();
    }


    public async Task<KitchenOrderTicket> AddAsync(KitchenOrderTicket ticket)
    {
        await _context.KitchenOrderTickets.AddAsync(ticket);
        await _context.SaveChangesAsync();

        return ticket;
    }

    public Task<OrderItem?> GetOrderItemAsync(int orderItemId, int orderId) =>
        _context.OrderItems
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.Id == orderItemId && x.OrderId == orderId);
}
