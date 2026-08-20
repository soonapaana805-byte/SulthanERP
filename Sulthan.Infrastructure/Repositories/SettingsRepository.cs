using Microsoft.EntityFrameworkCore;
using Sulthan.Core.Entities;
using Sulthan.Core.Interfaces;
using Sulthan.Infrastructure.Data;

namespace Sulthan.Infrastructure.Repositories;

public class SettingsRepository : ISettingsRepository
{
    private readonly RestaurantDbContext _context;

    public SettingsRepository(RestaurantDbContext context)
    {
        _context = context;
    }

    public async Task<Settings?> GetAsync()
    {
        var settings = await _context.Settings.FirstOrDefaultAsync();

        if (settings != null)
            return settings;

        settings = new Settings
        {
            ShopName = "Sulthan Hotel",
            Address = "Main Bus Stand, Thiyagadurgam",
            Phone = "",
            Email = "",
            Website = "",
            GstNumber = "",
            CurrencySymbol = "₹",
            DecimalPlaces = 2,
            ShowTaxOnCustomerBill = false,
            ShowGstNumberOnBill = false,
            ShowShopPhoneOnBill = true,
            ShowShopAddressOnBill = true,
            PrinterWidth = "80MM",
            AutoPrintAfterPayment = true,
            HeaderMessage = "",
            FooterMessage = "Thank You • Visit Again",
            TimeZone = "Asia/Kolkata",
            IsRestaurantOpen = true,
            IsActive = true,
            CreatedOn = DateTime.Now
        };

        await _context.Settings.AddAsync(settings);
        await _context.SaveChangesAsync();

        return settings;
    }

    public async Task<Settings> UpdateAsync(Settings settings)
    {
        settings.UpdatedOn = DateTime.Now;

        _context.Settings.Update(settings);
        await _context.SaveChangesAsync();

        return settings;
    }
}