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
            .Include(x => x.Order)
            .Include(x => x.Order.Items)
            .FirstOrDefaultAsync(x => x.Id == id);
    }


    public async Task<List<KitchenOrderTicket>> GetAllAsync()
    {
        return await _context.KitchenOrderTickets
            .Include(x => x.Order)
            .OrderByDescending(x => x.CreatedOn)
            .ToListAsync();
    }


    public async Task<KitchenOrderTicket> AddAsync(KitchenOrderTicket ticket)
    {
        await _context.KitchenOrderTickets.AddAsync(ticket);
        await _context.SaveChangesAsync();

        return ticket;
    }
}