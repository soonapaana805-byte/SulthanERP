using Microsoft.EntityFrameworkCore;
using Sulthan.Core.DTOs.Dashboard;
using Sulthan.Core.Interfaces;
using Sulthan.Infrastructure.Data;

namespace Sulthan.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly RestaurantDbContext _context;

    public DashboardService(RestaurantDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardResponseDto> GetDashboardAsync()
    {
        var today = DateTime.Today;

        var todayPayments = await _context.Payments.ToListAsync();

        var todayOrders = await _context.Orders
            .Include(x => x.Items)
            .ThenInclude(i => i.MenuItem)
            .ToListAsync();

        return new DashboardResponseDto
        {
            TodaySales = todayPayments.Sum(x => x.GrandTotal),

            TodayBills = todayPayments.Count,

            TodayCollection = todayPayments.Sum(x => x.PaidAmount),

            PendingCollection = todayPayments.Sum(x => x.BalanceAmount),

            TotalTables = await _context.DiningTables.CountAsync(),

            AvailableTables = await _context.DiningTables.CountAsync(x => x.Status == "Available"),

            OccupiedTables = await _context.DiningTables.CountAsync(x => x.Status == "Occupied"),

            ReservedTables = await _context.DiningTables.CountAsync(x => x.Status == "Reserved"),

            TotalMenuItems = await _context.MenuItems.CountAsync(),

            AvailableMenuItems = await _context.MenuItems.CountAsync(x => x.IsAvailable),

            TotalCategories = await _context.Categories.CountAsync(),

            TopSellingItems = todayOrders
                .SelectMany(x => x.Items)
                .GroupBy(x => x.MenuItem!.Name)
                .Select(g => new TopSellingItemDto
                {
                    ItemName = g.Key,
                    QuantitySold = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.QuantitySold)
                .Take(5)
                .ToList()
        };
    }
}