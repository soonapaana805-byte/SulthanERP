using System.Data;
using Microsoft.EntityFrameworkCore;
using Sulthan.Core.Common;
using Sulthan.Core.DTOs.CaptainOrders;
using Sulthan.Core.DTOs.Orders;
using Sulthan.Core.DTOs.PendingOrders;
using Sulthan.Core.Entities;
using Sulthan.Core.Enums;
using Sulthan.Core.Interfaces;
using Sulthan.Infrastructure.Data;

namespace Sulthan.Infrastructure.Services;

/// <summary>
/// Owns the Captain-only dine-in lifecycle: Available -> Occupied -> BillRequested.
/// Cashier checkout remains in PendingOrderService and moves the table to CleaningPending.
/// </summary>
public sealed class CaptainOrderService : ICaptainOrderService
{
    private readonly RestaurantDbContext _context;
    private readonly IBillCounterRepository _billCounterRepository;

    public CaptainOrderService(
        RestaurantDbContext context,
        IBillCounterRepository billCounterRepository)
    {
        _context = context;
        _billCounterRepository = billCounterRepository;
    }

    public async Task<IReadOnlyList<PendingOrderDto>> GetOpenOrdersAsync(
        CancellationToken cancellationToken = default)
    {
        var orders = await OpenOrdersQuery()
            .OrderBy(x => x.DiningTable!.DisplayOrder)
            .ThenBy(x => x.CreatedOn)
            .ToListAsync(cancellationToken);

        return await MapOrdersAsync(orders, cancellationToken);
    }

    public async Task<PendingOrderDto?> GetByIdAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await OpenOrdersQuery()
            .SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken);

        return order is null
            ? null
            : await MapOrderAsync(order, cancellationToken);
    }

    public async Task<PendingOrderDto?> GetByTableAsync(
        int diningTableId,
        CancellationToken cancellationToken = default)
    {
        var order = await OpenOrdersQuery()
            .SingleOrDefaultAsync(x => x.DiningTableId == diningTableId, cancellationToken);

        return order is null
            ? null
            : await MapOrderAsync(order, cancellationToken);
    }

    public async Task<PendingOrderDto> CreateAsync(
        CreateCaptainOrderDto dto,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateItems(dto.Items);

        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var captain = await GetCaptainAsync(authenticatedUserId, cancellationToken);
            var table = await _context.DiningTables
                .SingleOrDefaultAsync(x => x.Id == dto.DiningTableId && x.IsActive, cancellationToken)
                ?? throw new ArgumentException("Dining table not found.");

            if (!DiningTableStatus.IsAvailable(table.Status))
                throw new InvalidOperationException($"Table {table.TableNumber} is not available.");

            var alreadyHasOpenOrder = await _context.Orders.AnyAsync(
                x => x.IsActive &&
                     x.BillStatus == OrderStatus.Pending &&
                     x.OrderType == OrderType.DineIn &&
                     x.DiningTableId == table.Id,
                cancellationToken);

            if (alreadyHasOpenOrder)
                throw new InvalidOperationException($"Table {table.TableNumber} already has an active order.");

            var menuItems = await GetMenuItemsAsync(dto.Items, cancellationToken);
            var order = new Order
            {
                BillNumber = await _billCounterRepository.GetNextBillNumberAsync(),
                OrderType = OrderType.DineIn,
                BillStatus = OrderStatus.Pending,
                DiningTableId = table.Id,
                DiningTable = table,
                UserId = captain.Id,
                User = captain,
                CustomerName = Clean(dto.CustomerName),
                Remarks = Clean(dto.Remarks),
                Discount = 0m,
                Tax = 0m
            };

            var newItems = BuildOrderItems(dto.Items, menuItems, table);
            foreach (var item in newItems)
                order.Items.Add(item);

            RecalculateTotals(order);

            var ticket = BuildKitchenTicket(order, newItems, 1);
            _context.Orders.Add(order);
            _context.KitchenOrderTickets.Add(ticket);

            table.Status = DiningTableStatus.Occupied;
            table.UpdatedOn = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return MapOrder(order, ticket);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new InvalidOperationException(
                "Captain order could not be saved. No order or KOT was created.", ex);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<PendingOrderDto> AddItemsAsync(
        int orderId,
        AddCaptainOrderItemsDto dto,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateItems(dto.Items);

        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            await GetCaptainAsync(authenticatedUserId, cancellationToken);

            var order = await _context.Orders
                .Include(x => x.DiningTable)
                .Include(x => x.User)
                .Include(x => x.Items)
                    .ThenInclude(x => x.MenuItem)
                .SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken)
                ?? throw new KeyNotFoundException("Captain order not found.");

            EnsureOrderCanAcceptItems(order);
            var table = order.DiningTable!;
            var menuItems = await GetMenuItemsAsync(dto.Items, cancellationToken);
            var newItems = BuildOrderItems(dto.Items, menuItems, table);

            foreach (var item in newItems)
                order.Items.Add(item);

            RecalculateTotals(order);
            order.UpdatedOn = DateTime.UtcNow;

            var existingTicketCount = await _context.KitchenOrderTickets
                .CountAsync(x => x.OrderId == order.Id, cancellationToken);

            if (existingTicketCount >= 99)
                throw new InvalidOperationException("This order has reached the maximum number of KOT additions.");

            var ticket = BuildKitchenTicket(order, newItems, existingTicketCount + 1);
            _context.KitchenOrderTickets.Add(ticket);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return MapOrder(order, ticket);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new InvalidOperationException(
                "Additional items could not be saved. No additional KOT was created.", ex);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<PendingOrderDto> RequestBillAsync(
        int orderId,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            await GetCaptainAsync(authenticatedUserId, cancellationToken);

            var order = await _context.Orders
                .Include(x => x.DiningTable)
                .Include(x => x.User)
                .Include(x => x.Items)
                    .ThenInclude(x => x.MenuItem)
                .SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken)
                ?? throw new KeyNotFoundException("Captain order not found.");

            EnsureOpenDineInOrder(order);
            var table = order.DiningTable!;

            var ticket = await _context.KitchenOrderTickets
                .AsNoTracking()
                .Where(x => x.OrderId == order.Id)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (order.BillPrintedOn.HasValue ||
                string.Equals(
                    table.Status,
                    DiningTableStatus.PaymentPending,
                    StringComparison.OrdinalIgnoreCase))
            {
                await transaction.CommitAsync(cancellationToken);
                return MapOrder(order, ticket);
            }

            var requestedOn = DateTime.UtcNow;
            if (string.Equals(
                    table.Status,
                    DiningTableStatus.Occupied,
                    StringComparison.OrdinalIgnoreCase))
            {
                table.Status = DiningTableStatus.BillRequested;
                table.UpdatedOn = requestedOn;
                order.BillRequestedOn ??= requestedOn;
                order.UpdatedOn = requestedOn;
            }
            else if (!string.Equals(
                         table.Status,
                         DiningTableStatus.BillRequested,
                         StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Table {table.TableNumber} is not ready for bill printing.");
            }

            var requestKey = $"pending-bill:{order.BillNumber}";
            var printAlreadyQueued = await _context.CustomerBillPrintJobs
                .AsNoTracking()
                .AnyAsync(
                    x => x.RequestKey == requestKey,
                    cancellationToken);
            var newPrintJobQueued = false;

            if (!printAlreadyQueued)
            {
                _context.CustomerBillPrintJobs.Add(
                    new CustomerBillPrintJob
                    {
                        OrderId = order.Id,
                        RequestedByUserId = authenticatedUserId,
                        DocumentType = CustomerBillDocumentType.PendingBill,
                        RequestKey = requestKey,
                        Status = CustomerBillPrintJobStatus.Pending
                    });
                newPrintJobQueued = true;
            }

            await _context.SaveChangesAsync(cancellationToken);

            var queuedJobExists = await _context.CustomerBillPrintJobs
                .AsNoTracking()
                .AnyAsync(
                    x => x.RequestKey == requestKey &&
                         x.DocumentType == CustomerBillDocumentType.PendingBill &&
                         (!newPrintJobQueued ||
                          x.Status == CustomerBillPrintJobStatus.Pending),
                    cancellationToken);
            if (!queuedJobExists)
            {
                throw new InvalidOperationException(
                    "The customer bill print job could not be queued.");
            }

            var result = MapOrder(order, ticket);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<PendingOrderDto> QueueRequestedBillPrintAsync(
        int orderId,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            await GetCaptainAsync(authenticatedUserId, cancellationToken);

            var order = await _context.Orders
                .Include(x => x.DiningTable)
                .Include(x => x.User)
                .Include(x => x.Items)
                    .ThenInclude(x => x.MenuItem)
                .SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken)
                ?? throw new KeyNotFoundException("Captain order not found.");

            EnsureOpenDineInOrder(order);
            var table = order.DiningTable!;
            var ticket = await _context.KitchenOrderTickets
                .AsNoTracking()
                .Where(x => x.OrderId == order.Id)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (order.BillPrintedOn.HasValue &&
                string.Equals(
                    table.Status,
                    DiningTableStatus.PaymentPending,
                    StringComparison.OrdinalIgnoreCase))
            {
                await transaction.CommitAsync(cancellationToken);
                return MapOrder(order, ticket);
            }

            if (!string.Equals(
                    table.Status,
                    DiningTableStatus.BillRequested,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Request the bill before queueing the customer print.");
            }

            var requestKey = $"pending-bill:{order.BillNumber}";
            var printAlreadyQueued = await _context.CustomerBillPrintJobs
                .AsNoTracking()
                .AnyAsync(
                    x => x.RequestKey == requestKey,
                    cancellationToken);

            if (!order.BillPrintedOn.HasValue && !printAlreadyQueued)
            {
                _context.CustomerBillPrintJobs.Add(
                    new CustomerBillPrintJob
                    {
                        OrderId = order.Id,
                        RequestedByUserId = authenticatedUserId,
                        DocumentType = CustomerBillDocumentType.PendingBill,
                        RequestKey = requestKey,
                        Status = CustomerBillPrintJobStatus.Pending
                    });

                await _context.SaveChangesAsync(cancellationToken);
            }

            var result = MapOrder(order, ticket);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private IQueryable<Order> OpenOrdersQuery() => _context.Orders
        .AsNoTracking()
        .Where(x => x.IsActive &&
                    x.BillStatus == OrderStatus.Pending &&
                    x.OrderType == OrderType.DineIn)
        .Include(x => x.DiningTable)
        .Include(x => x.Customer)
        .Include(x => x.User)
        .Include(x => x.Items)
            .ThenInclude(x => x.MenuItem);

    private async Task<List<PendingOrderDto>> MapOrdersAsync(
        IReadOnlyCollection<Order> orders,
        CancellationToken cancellationToken)
    {
        if (orders.Count == 0)
            return [];

        var orderIds = orders.Select(x => x.Id).ToList();
        var tickets = await _context.KitchenOrderTickets
            .AsNoTracking()
            .Where(x => orderIds.Contains(x.OrderId))
            .OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

        var latestTicketByOrder = tickets
            .GroupBy(x => x.OrderId)
            .ToDictionary(x => x.Key, x => x.First());

        return orders
            .Select(x => MapOrder(x, latestTicketByOrder.GetValueOrDefault(x.Id)))
            .ToList();
    }

    private async Task<PendingOrderDto> MapOrderAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        var ticket = await _context.KitchenOrderTickets
            .AsNoTracking()
            .Where(x => x.OrderId == order.Id)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return MapOrder(order, ticket);
    }

    private async Task<User> GetCaptainAsync(
        int authenticatedUserId,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users.SingleOrDefaultAsync(
            x => x.Id == authenticatedUserId && x.IsActive,
            cancellationToken)
            ?? throw new UnauthorizedAccessException("The signed-in Captain no longer exists.");

        if (user.Role is not (UserRole.Captain or UserRole.Admin))
            throw new UnauthorizedAccessException("Only a Captain can manage table orders.");

        return user;
    }

    private async Task<Dictionary<int, MenuItem>> GetMenuItemsAsync(
        IEnumerable<AddOrderItemDto> requestedItems,
        CancellationToken cancellationToken)
    {
        var ids = requestedItems.Select(x => x.MenuItemId).Distinct().ToList();
        var menuItems = await _context.MenuItems
            .Where(x => ids.Contains(x.Id) && x.IsActive)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        if (menuItems.Count != ids.Count)
            throw new ArgumentException("One or more menu items were not found.");

        foreach (var menuItem in menuItems.Values)
        {
            if (!menuItem.IsAvailable)
                throw new InvalidOperationException($"{menuItem.Name} is currently unavailable.");
        }

        return menuItems;
    }

    private static List<OrderItem> BuildOrderItems(
        IEnumerable<AddOrderItemDto> requestedItems,
        IReadOnlyDictionary<int, MenuItem> menuItems,
        DiningTable table)
    {
        var isAcTable = string.Equals(table.TableType, "AC", StringComparison.OrdinalIgnoreCase);

        return requestedItems.Select(requestedItem =>
        {
            var menuItem = menuItems[requestedItem.MenuItemId];
            return new OrderItem
            {
                MenuItemId = menuItem.Id,
                MenuItem = menuItem,
                Quantity = requestedItem.Quantity,
                Price = isAcTable ? menuItem.ACPrice : menuItem.NonACPrice,
                Notes = Clean(requestedItem.Notes)
            };
        }).ToList();
    }

    private static KitchenOrderTicket BuildKitchenTicket(
        Order order,
        IEnumerable<OrderItem> newItems,
        int sequence)
    {
        var kotNumber = sequence == 1
            ? $"KOT-{order.BillNumber}"
            : $"KOT-{order.BillNumber}-{sequence:D2}";

        return new KitchenOrderTicket
        {
            KotNumber = kotNumber,
            Order = order,
            PrintedOn = DateTime.Now,
            IsReprint = false,
            Items = newItems.Select(x => new KitchenOrderTicketItem
            {
                OrderItem = x,
                MenuItemId = x.MenuItemId,
                Quantity = x.Quantity,
                Notes = x.Notes
            }).ToList()
        };
    }

    private static void RecalculateTotals(Order order)
    {
        order.SubTotal = order.Items.Sum(
            x => x.Price * (x.Quantity - x.CancelledQuantity));
        order.GrandTotal = order.SubTotal - order.Discount + order.Tax;
    }

    private static void EnsureOrderCanAcceptItems(Order order)
    {
        EnsureOpenDineInOrder(order);
        var table = order.DiningTable!;

        if (!string.Equals(table.Status, DiningTableStatus.Occupied, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Table {table.TableNumber} cannot accept items after its bill has been requested.");
    }

    private static void EnsureOpenDineInOrder(Order order)
    {
        if (!order.IsActive || order.BillStatus != OrderStatus.Pending || order.OrderType != OrderType.DineIn)
            throw new InvalidOperationException("This Captain order is no longer active.");

        if (order.DiningTable is null)
            throw new InvalidOperationException("Dining table not found for this order.");
    }

    private static void ValidateItems(IReadOnlyCollection<AddOrderItemDto>? items)
    {
        if (items is null || items.Count == 0)
            throw new ArgumentException("At least one order item is required.");

        foreach (var item in items)
        {
            if (item.MenuItemId <= 0 || item.Quantity is < 1 or > 100)
                throw new ArgumentException("Each order item must have a valid menu item and quantity.");
        }
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static PendingOrderDto MapOrder(Order order, KitchenOrderTicket? ticket)
    {
        return new PendingOrderDto
        {
            OrderId = order.Id,
            BillNumber = order.BillNumber,
            OrderType = order.OrderType,
            DiningTableId = order.DiningTableId,
            TableNumber = order.DiningTable?.TableNumber,
            TableStatus = order.DiningTable?.Status,
            CustomerId = order.CustomerId,
            CustomerName = order.CustomerName ?? order.Customer?.Name,
            CreatedByUserId = order.UserId,
            CaptainName = order.User?.FullName,
            CreatedOn = order.CreatedOn,
            SubTotal = order.SubTotal,
            Discount = order.Discount,
            Tax = order.Tax,
            GrandTotal = order.GrandTotal,
            Remarks = order.Remarks,
            KitchenTicketNumber = ticket?.KotNumber ?? string.Empty,
            BillRequestedOn = order.BillRequestedOn,
            BillPrintedOn = order.BillPrintedOn,
            Items = order.Items
                .Where(x => x.Quantity > x.CancelledQuantity)
                .Select(x => new PendingOrderItemDto
            {
                MenuItemId = x.MenuItemId,
                ItemName = x.MenuItem?.Name ?? string.Empty,
                Price = x.Price,
                Quantity = x.Quantity - x.CancelledQuantity,
                Notes = x.Notes
            }).ToList()
        };
    }
}
