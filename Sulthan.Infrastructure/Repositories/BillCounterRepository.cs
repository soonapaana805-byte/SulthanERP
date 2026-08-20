using Microsoft.EntityFrameworkCore;
using Sulthan.Core.Entities;
using Sulthan.Core.Interfaces;
using Sulthan.Infrastructure.Data;

namespace Sulthan.Infrastructure.Repositories;

public class BillCounterRepository : IBillCounterRepository
{
    private readonly RestaurantDbContext _context;

    public BillCounterRepository(RestaurantDbContext context)
    {
        _context = context;
    }

    public async Task<string> GetNextBillNumberAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var counter = await _context.BillCounters
            .FirstOrDefaultAsync(x => x.BusinessDate == today);

        if (counter == null)
        {
            counter = new BillCounter
            {
                BusinessDate = today,
                LastBillNumber = 0
            };

            _context.BillCounters.Add(counter);
        }

        counter.LastBillNumber++;

        await _context.SaveChangesAsync();

        return $"{today:yyyyMMdd}-{counter.LastBillNumber:D3}";
    }

    public async Task<string> GetNextBillNumberPreviewAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var lastBillNumber = await _context.BillCounters
            .AsNoTracking()
            .Where(x => x.BusinessDate == today)
            .Select(x => (int?)x.LastBillNumber)
            .SingleOrDefaultAsync(cancellationToken) ?? 0;

        return $"{today:yyyyMMdd}-{lastBillNumber + 1:D3}";
    }
}
