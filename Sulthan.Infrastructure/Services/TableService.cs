using Sulthan.Core.DTOs.Tables;
using Sulthan.Core.Entities;
using Sulthan.Core.Interfaces;

namespace Sulthan.Infrastructure.Services;

public class TableService : ITableService
{
    private readonly ITableRepository _repository;

    public TableService(ITableRepository repository)
    {
        _repository = repository;
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
            Status = "Available"
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

    public async Task DeleteAsync(int id)
    {
        var table = await _repository.GetByIdAsync(id);

        if (table == null)
            throw new Exception("Table not found.");

        await _repository.DeleteAsync(table);
    }
}