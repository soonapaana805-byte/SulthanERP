using Sulthan.Core.Entities;

namespace Sulthan.Core.Interfaces;

public interface ISettingsRepository
{
    Task<Settings?> GetAsync();

    Task<Settings> UpdateAsync(Settings settings);
}