using System.Data;
using Microsoft.EntityFrameworkCore;
using Sulthan.Core.Common;
using Sulthan.Core.DTOs.Tables;
using Sulthan.Core.Entities;
using Sulthan.Core.Enums;
using Sulthan.Core.Interfaces;
using Sulthan.Infrastructure.Data;

namespace Sulthan.Infrastructure.Services;

public class TableService : ITableService
{
    private readonly ITableRepository _repository;
    private readonly RestaurantDbContext _context;

    public TableService(ITableRepository repository, RestaurantDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<List<DiningTable>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<DiningTable?> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<DiningTable> CreateAsync(CreateDiningTableDto dto)
    {
        var table = new DiningTable
        {
            TableNumber = dto.TableNumber,
            TableType = dto.TableType,
            Capacity = dto.Capacity,
            DisplayOrder = dto.DisplayOrder,
            Status = DiningTableStatus.Available
        };

        await _repository.AddAsync(table);

        return table;
    }

    public async Task<DiningTable> UpdateAsync(int id, UpdateDiningTableDto dto)
    {
        var table = await _repository.GetByIdAsync(id);

        if (table == null)
            throw new Exception("Table not found.");

        table.TableNumber = dto.TableNumber;
        table.TableType = dto.TableType;
        table.Capacity = dto.Capacity;
        table.Status = dto.Status;
        table.DisplayOrder = dto.DisplayOrder;
        table.IsActive = dto.IsActive;

        await _repository.UpdateAsync(table);

        return table;
    }

    public async Task<DiningTable> MarkCleanAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var table = await _context.DiningTables
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (table is null)
                throw new InvalidOperationException("Table not found.");

            if (!table.IsActive)
                throw new InvalidOperationException("Inactive tables cannot be marked clean.");

            if (!string.Equals(table.Status, DiningTableStatus.CleaningPending, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only a paid table awaiting cleaning can be marked clean.");

            var hasPendingDineInOrder = await _context.Orders
                .AsNoTracking()
                .AnyAsync(
                    x => x.IsActive &&
                         x.DiningTableId == id &&
                         x.OrderType == OrderType.DineIn &&
                         x.BillStatus == OrderStatus.Pending,
                    cancellationToken);

            if (hasPendingDineInOrder)
                throw new InvalidOperationException("This table still has a payment-pending order.");

            table.Status = DiningTableStatus.Available;
            table.UpdatedOn = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return table;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        var table = await _repository.GetByIdAsync(id);

        if (table == null)
            throw new Exception("Table not found.");

        await _repository.DeleteAsync(table);
    }
}
