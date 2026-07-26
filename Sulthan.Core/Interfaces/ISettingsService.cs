using Sulthan.Core.Entities;

namespace Sulthan.Core.Interfaces;

public interface ISettingsService
{
    Task<Settings?> GetAsync();

    Task<Settings> UpdateAsync(Settings settings);
}