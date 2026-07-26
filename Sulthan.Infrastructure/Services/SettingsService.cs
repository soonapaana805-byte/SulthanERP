using Sulthan.Core.Entities;
using Sulthan.Core.Interfaces;

namespace Sulthan.Infrastructure.Services;

public class SettingsService : ISettingsService
{
    private readonly ISettingsRepository _settingsRepository;

    public SettingsService(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public async Task<Settings?> GetAsync()
    {
        return await _settingsRepository.GetAsync();
    }

    public async Task<Settings> UpdateAsync(Settings settings)
    {
        return await _settingsRepository.UpdateAsync(settings);
    }
}