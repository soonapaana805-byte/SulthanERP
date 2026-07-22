using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Sulthan.Core.Entities;
using Sulthan.Core.Enums;

namespace Sulthan.Infrastructure.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(RestaurantDbContext context)
        {
            await context.Database.MigrateAsync();

            if (!await context.Users.AnyAsync())
            {
                var admin = new User
                {
                    FullName = "Administrator",
                    UserName = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    Role = UserRole.Admin,
                    IsActive = true
                };

                context.Users.Add(admin);
            }

            if (!await context.BillCounters.AnyAsync())
            {
                context.BillCounters.Add(new BillCounter
                {
                    BusinessDate = DateOnly.FromDateTime(DateTime.Today),
                    LastBillNumber = 0
                });
            }

            await context.SaveChangesAsync();
        }
    }
}