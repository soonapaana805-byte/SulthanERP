using Sulthan.Core.Entities;

namespace Sulthan.Core.Interfaces;

public interface ITableRepository
{
    Task<List<DiningTable>> GetAllAsync();

    Task<DiningTable?> GetByIdAsync(int id);

    Task<DiningTable?> GetByTableNumberAsync(string tableNumber);

    Task AddAsync(DiningTable table);

    Task UpdateAsync(DiningTable table);

    Task DeleteAsync(DiningTable table);
}