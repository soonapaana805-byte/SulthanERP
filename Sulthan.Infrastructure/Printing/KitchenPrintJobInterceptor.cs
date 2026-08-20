using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Sulthan.Core.Common;
using Sulthan.Core.Entities;
using Sulthan.Infrastructure.Data;

namespace Sulthan.Infrastructure.Printing;

/// <summary>
/// Adds durable kitchen print jobs in the same database transaction as every new KOT.
/// </summary>
public sealed class KitchenPrintJobInterceptor : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not RestaurantDbContext context)
            return result;

        var newTickets = context.ChangeTracker
            .Entries<KitchenOrderTicket>()
            .Where(entry => entry.State == EntityState.Added)
            .Select(entry => entry.Entity)
            .ToList();

        if (newTickets.Count == 0)
            return result;

        var menuItemIds = newTickets
            .SelectMany(ticket => ticket.Items)
            .Select(item => item.MenuItemId)
            .Distinct()
            .ToList();

        var kitchenNamesByMenuItemId = await context.MenuItems
            .AsNoTracking()
            .Where(item => menuItemIds.Contains(item.Id))
            .ToDictionaryAsync(
                item => item.Id,
                item => item.KitchenName,
                cancellationToken);

        foreach (var ticket in newTickets)
        {
            var kitchenNames = ticket.Items
                .Select(item => ResolveKitchenName(
                    kitchenNamesByMenuItemId.GetValueOrDefault(item.MenuItemId)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (kitchenNames.Count == 0)
                kitchenNames.Add("Main Kitchen");

            foreach (var kitchenName in kitchenNames)
            {
                if (HasTrackedJob(context.ChangeTracker, ticket, kitchenName))
                    continue;

                context.KitchenPrintJobs.Add(new KitchenPrintJob
                {
                    KitchenOrderTicket = ticket,
                    KitchenName = kitchenName,
                    DocumentType = KitchenPrintDocumentType.OriginalKot,
                    Status = KitchenPrintJobStatus.Pending
                });
            }
        }

        return result;
    }

    private static bool HasTrackedJob(
        ChangeTracker changeTracker,
        KitchenOrderTicket ticket,
        string kitchenName)
    {
        return changeTracker
            .Entries<KitchenPrintJob>()
            .Any(entry =>
                entry.State != EntityState.Deleted &&
                ReferenceEquals(entry.Entity.KitchenOrderTicket, ticket) &&
                entry.Entity.DocumentType == KitchenPrintDocumentType.OriginalKot &&
                string.Equals(
                    entry.Entity.KitchenName,
                    kitchenName,
                    StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolveKitchenName(string? kitchenName)
    {
        return string.IsNullOrWhiteSpace(kitchenName)
            ? "Main Kitchen"
            : kitchenName.Trim();
    }
}
