using Sulthan.Core.DTOs.Tables;
using Sulthan.Core.Entities;

namespace Sulthan.Core.Interfaces;

public interface ITableService
{
    Task<List<DiningTable>> GetAllAsync();

    Task<DiningTable?> GetByIdAsync(int id);

    Task<DiningTable> CreateAsync(CreateDiningTableDto dto);

    Task<DiningTable> UpdateAsync(int id, UpdateDiningTableDto dto);

    Task DeleteAsync(int id);
}