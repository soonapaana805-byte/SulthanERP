using Microsoft.EntityFrameworkCore;
using Sulthan.Core.Entities;
using Sulthan.Core.Interfaces;
using Sulthan.Infrastructure.Data;

namespace Sulthan.Infrastructure.Repositories;

public class TableRepository : ITableRepository
{
    private readonly RestaurantDbContext _context;

    public TableRepository(RestaurantDbContext context)
    {
        _context = context;
    }

    public async Task<List<DiningTable>> GetAllAsync()
    {
        return await _context.DiningTables
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync();
    }

    public async Task<DiningTable?> GetByIdAsync(int id)
    {
        return await _context.DiningTables
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<DiningTable?> GetByTableNumberAsync(string tableNumber)
    {
        return await _context.DiningTables
            .FirstOrDefaultAsync(x => x.TableNumber == tableNumber);
    }

    public async Task AddAsync(DiningTable table)
    {
        await _context.DiningTables.AddAsync(table);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(DiningTable table)
    {
        _context.DiningTables.Update(table);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(DiningTable table)
    {
        _context.DiningTables.Remove(table);
        await _context.SaveChangesAsync();
    }
}