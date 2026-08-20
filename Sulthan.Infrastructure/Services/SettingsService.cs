using Sulthan.Core.Entities;
using Sulthan.Core.Interfaces;

namespace Sulthan.Infrastructure.Services;

public class SettingsService : ISettingsService
{
    private readonly ISettingsRepository _settingsRepository;

    public SettingsService(
        ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public async Task<Settings?> GetAsync()
    {
        return await _settingsRepository.GetAsync();
    }

    public async Task<Settings> UpdateAsync(
        Settings settings)
    {
        if (settings == null)
        {
            throw new ArgumentException(
                "Settings data is required.");
        }

        if (string.IsNullOrWhiteSpace(settings.ShopName))
        {
            throw new ArgumentException(
                "Shop name is required.");
        }

        var printerWidth = settings.PrinterWidth?.Trim().ToUpperInvariant();
        if (printerWidth is not ("58MM" or "80MM"))
        {
            throw new ArgumentException(
                "Printer width must be either 58MM or 80MM.");
        }

        settings.PrinterWidth = printerWidth;

        return await _settingsRepository
            .UpdateAsync(settings);
    }
}
