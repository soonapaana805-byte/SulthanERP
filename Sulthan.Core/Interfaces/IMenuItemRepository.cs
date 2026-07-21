using Sulthan.Core.Entities;

namespace Sulthan.Core.Interfaces;

public interface IMenuItemRepository
{
    Task<List<MenuItem>> GetAllAsync();

    Task<MenuItem?> GetByIdAsync(int id);

    Task<MenuItem?> GetByNameAsync(string name);

    Task AddAsync(MenuItem menuItem);

    Task UpdateAsync(MenuItem menuItem);

    Task DeleteAsync(MenuItem menuItem);
}