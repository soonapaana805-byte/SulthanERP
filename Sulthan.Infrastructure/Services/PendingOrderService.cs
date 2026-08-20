using System.Data;
using Microsoft.EntityFrameworkCore;
using Sulthan.Core.Common;
using Sulthan.Core.DTOs.Billing;
using Sulthan.Core.DTOs.Checkout;
using Sulthan.Core.DTOs.PendingOrders;
using Sulthan.Core.Entities;
using Sulthan.Core.Enums;
using Sulthan.Core.Interfaces;
using Sulthan.Infrastructure.Data;

namespace Sulthan.Infrastructure.Services;

/// <summary>
/// Handles phone/unpaid orders. Creation saves the order and its KOT together; payment is added only
/// when the existing pending order is later checked out.
/// </summary>
public sealed class PendingOrderService : IPendingOrderService
{
    private readonly RestaurantDbContext _context;
    private readonly IBillCounterRepository _billCounterRepository;
    private readonly IReceiptFormatter _receiptFormatter;
    private readonly IAuthService _authService;

    public PendingOrderService(
        RestaurantDbContext context,
        IBillCounterRepository billCounterRepository,
        IReceiptFormatter receiptFormatter,
        IAuthService authService)
    {
        _context = context;
        _billCounterRepository = billCounterRepository;
        _receiptFormatter = receiptFormatter;
        _authService = authService;
    }

    public async Task<PendingOrderDto> CreateAsync(
        CreatePendingOrderDto dto,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateCreateRequest(dto);

        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            await EnsureCashierExistsAsync(authenticatedUserId, cancellationToken);

            var diningTable = await ResolveDiningTableAsync(
                dto.OrderType,
                dto.DiningTableId,
                cancellationToken);

            var customerName = await ResolveCustomerNameAsync(
                dto.CustomerId,
                dto.CustomerName,
                cancellationToken);

            var menuItems = await GetMenuItemsAsync(dto.Items.Select(x => x.MenuItemId), cancellationToken);

            var order = new Order
            {
                BillNumber = await _billCounterRepository.GetNextBillNumberAsync(),
                OrderType = dto.OrderType,
                BillStatus = OrderStatus.Pending,
                DiningTableId = dto.DiningTableId,
                DiningTable = diningTable,
                CustomerId = dto.CustomerId,
                CustomerName = customerName,
                UserId = authenticatedUserId,
                Discount = dto.Discount,
                Tax = dto.Tax,
                Remarks = string.IsNullOrWhiteSpace(dto.Remarks) ? null : dto.Remarks.Trim()
            };

            foreach (var requestedItem in dto.Items)
            {
                var menuItem = menuItems[requestedItem.MenuItemId];

                if (!menuItem.IsAvailable)
                    throw new InvalidOperationException($"{menuItem.Name} is currently unavailable.");

                if (dto.OrderType == OrderType.Parcel && !menuItem.IsParcelAvailable)
                    throw new InvalidOperationException($"{menuItem.Name} is not available for take away.");

                order.Items.Add(new OrderItem
                {
                    MenuItemId = menuItem.Id,
                    Quantity = requestedItem.Quantity,
                    Price = GetItemPrice(menuItem, dto.OrderType, diningTable),
                    Notes = requestedItem.Notes
                });
            }

            order.SubTotal = order.Items.Sum(x => x.Price * x.Quantity);

            if (order.Discount >= order.SubTotal && order.Discount > 0)
                throw new ArgumentException("Discount must be less than the subtotal.");

            order.GrandTotal = order.SubTotal - order.Discount + order.Tax;

            if (order.Discount > 0)
            {
                var approval = dto.DiscountApproval
                    ?? throw new UnauthorizedAccessException(
                        "Valid active Admin approval is required for a non-zero discount.");

                var approvedByUserId = await _authService.ValidateActiveAdminAsync(
                    approval,
                    cancellationToken);

                _context.DiscountAudits.Add(new DiscountAudit
                {
                    Order = order,
                    SubTotal = order.SubTotal,
                    PreviousDiscount = 0m,
                    ApprovedDiscount = order.Discount,
                    GrandTotal = order.GrandTotal,
                    Reason = approval.Reason.Trim(),
                    RequestedByUserId = authenticatedUserId,
                    ApprovedByUserId = approvedByUserId,
                    CreatedOn = DateTime.UtcNow
                });
            }

            var createdAt = DateTime.Now;
            var kitchenTicket = new KitchenOrderTicket
            {
                KotNumber = $"KOT-{order.BillNumber}",
                Order = order,
                PrintedOn = createdAt,
                IsReprint = false,
                Items = order.Items.Select(x => new KitchenOrderTicketItem
                {
                    OrderItem = x,
                    MenuItemId = x.MenuItemId,
                    Quantity = x.Quantity,
                    Notes = x.Notes
                }).ToList()
            };

            _context.Orders.Add(order);
            _context.KitchenOrderTickets.Add(kitchenTicket);

            if (diningTable is not null)
            {
                diningTable.Status = DiningTableStatus.PaymentPending;
                diningTable.UpdatedOn = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return MapPendingOrder(order, kitchenTicket);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new InvalidOperationException(
                "Pending order could not be saved. No order or kitchen ticket was created.", ex);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<IReadOnlyList<PendingOrderDto>> GetPendingAsync(
        CancellationToken cancellationToken = default)
    {
        var orders = await _context.Orders
            .AsNoTracking()
            .Where(x => x.IsActive && x.BillStatus == OrderStatus.Pending)
            .Include(x => x.DiningTable)
            .Include(x => x.Customer)
            .Include(x => x.User)
            .Include(x => x.Items)
                .ThenInclude(x => x.MenuItem)
            .OrderByDescending(x => x.CreatedOn)
            .ToListAsync(cancellationToken);

        if (orders.Count == 0)
            return [];

        var orderIds = orders.Select(x => x.Id).ToList();
        var tickets = await _context.KitchenOrderTickets
            .AsNoTracking()
            .Where(x => orderIds.Contains(x.OrderId))
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var ticketsByOrderId = tickets
            .GroupBy(x => x.OrderId)
            .ToDictionary(x => x.Key, x => x.First());

        return orders
            .Select(order => MapPendingOrder(
                order,
                ticketsByOrderId.GetValueOrDefault(order.Id)))
            .ToList();
    }

    public async Task<PendingOrderDto?> GetByIdAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Where(x => x.Id == orderId && x.IsActive && x.BillStatus == OrderStatus.Pending)
            .Include(x => x.DiningTable)
            .Include(x => x.Customer)
            .Include(x => x.User)
            .Include(x => x.Items)
                .ThenInclude(x => x.MenuItem)
            .SingleOrDefaultAsync(cancellationToken);

        if (order is null)
            return null;

        var kitchenTicket = await _context.KitchenOrderTickets
            .AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return MapPendingOrder(order, kitchenTicket);
    }

    public async Task<PendingOrderPrintPreviewDto> GetBillPrintPreviewAsync(
        int orderId,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        var cashier = await GetCashierAsync(authenticatedUserId, cancellationToken);
        var order = await _context.Orders
            .AsNoTracking()
            .Include(x => x.DiningTable)
            .Include(x => x.Customer)
            .Include(x => x.User)
            .Include(x => x.Items)
                .ThenInclude(x => x.MenuItem)
            .SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            ?? throw new KeyNotFoundException("Pending table order not found.");

        EnsureBillCanBePrinted(order);

        var receiptText = await _receiptFormatter.GenerateReceiptAsync(
            new PrintBillDto
            {
                DocumentTitle = "CUSTOMER BILL",
                BillNumber = order.BillNumber,
                BillDate = (order.BillRequestedOn ?? DateTime.UtcNow).ToLocalTime(),
                TableNumber = order.DiningTable?.TableNumber ?? string.Empty,
                CustomerName = order.CustomerName ?? order.Customer?.Name,
                CaptainName = order.User?.FullName ?? string.Empty,
                CashierName = cashier.FullName,
                OrderType = ResolveReceiptOrderMode(order),
                SubTotal = order.SubTotal,
                Discount = order.Discount,
                Tax = order.Tax,
                GrandTotal = order.GrandTotal,
                PaidAmount = 0m,
                BalanceAmount = order.GrandTotal,
                PaymentMethod = "PAYMENT PENDING",
                Items = order.Items
                    .Where(x => x.Quantity > x.CancelledQuantity)
                    .Select(x => new BillItemDto
                {
                    MenuItemId = x.MenuItemId,
                    ItemName = x.MenuItem?.Name ?? string.Empty,
                    Price = x.Price,
                    Quantity = x.Quantity - x.CancelledQuantity,
                    Total = x.Price * (x.Quantity - x.CancelledQuantity)
                }).ToList()
            });

        return new PendingOrderPrintPreviewDto
        {
            OrderId = order.Id,
            BillNumber = order.BillNumber,
            ReceiptText = receiptText
        };
    }

    public async Task<PendingOrderDto> MarkBillPrintedAsync(
        int orderId,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            await EnsureCashierExistsAsync(authenticatedUserId, cancellationToken);

            var order = await _context.Orders
                .Include(x => x.DiningTable)
                .Include(x => x.Customer)
                .Include(x => x.User)
                .Include(x => x.Items)
                    .ThenInclude(x => x.MenuItem)
                .SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken)
                ?? throw new KeyNotFoundException("Pending table order not found.");

            EnsureOpenDineInOrder(order);
            var table = order.DiningTable!;

            if (string.Equals(table.Status, DiningTableStatus.PaymentPending, StringComparison.OrdinalIgnoreCase) &&
                order.BillPrintedOn.HasValue)
            {
                var existingTicket = await GetFirstKitchenTicketAsync(order.Id, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return MapPendingOrder(order, existingTicket);
            }

            if (!string.Equals(table.Status, DiningTableStatus.BillRequested, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The Captain has not requested this table bill.");

            var requestKey = $"pending-bill:{order.BillNumber}";
            var printAlreadyQueued = await _context.CustomerBillPrintJobs
                .AsNoTracking()
                .AnyAsync(x => x.RequestKey == requestKey, cancellationToken);

            if (!printAlreadyQueued)
            {
                _context.CustomerBillPrintJobs.Add(new CustomerBillPrintJob
                {
                    OrderId = order.Id,
                    RequestedByUserId = authenticatedUserId,
                    DocumentType = CustomerBillDocumentType.PendingBill,
                    RequestKey = requestKey,
                    Status = CustomerBillPrintJobStatus.Pending
                });
            }

            order.BillRequestedOn ??= DateTime.UtcNow;
            order.UpdatedOn = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            var kitchenTicket = await GetFirstKitchenTicketAsync(order.Id, cancellationToken);
            var result = MapPendingOrder(order, kitchenTicket);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<PendingOrderDto> QueueBillReprintAsync(
        int orderId,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCashierExistsAsync(authenticatedUserId, cancellationToken);

        var order = await _context.Orders
            .Include(x => x.DiningTable)
            .Include(x => x.Customer)
            .Include(x => x.User)
            .Include(x => x.Items)
                .ThenInclude(x => x.MenuItem)
            .SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            ?? throw new KeyNotFoundException("Pending table order not found.");

        EnsureOpenDineInOrder(order);
        if (order.BillPrintedOn is null ||
            !string.Equals(
                order.DiningTable!.Status,
                DiningTableStatus.PaymentPending,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The initial customer bill must print successfully before it can be reprinted.");
        }

        _context.CustomerBillPrintJobs.Add(new CustomerBillPrintJob
        {
            OrderId = order.Id,
            RequestedByUserId = authenticatedUserId,
            DocumentType = CustomerBillDocumentType.PendingBill,
            IsReprint = true,
            RequestKey = $"pending-bill-reprint:{order.BillNumber}:{Guid.NewGuid():N}",
            Status = CustomerBillPrintJobStatus.Pending
        });

        await _context.SaveChangesAsync(cancellationToken);
        var kitchenTicket = await GetFirstKitchenTicketAsync(
            order.Id,
            cancellationToken);
        return MapPendingOrder(order, kitchenTicket);
    }

    public async Task<CheckoutResponseDto> CheckoutAsync(
        int orderId,
        PendingOrderCheckoutDto dto,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateCheckoutRequest(dto);

        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            await EnsureCashierExistsAsync(authenticatedUserId, cancellationToken);

            var order = await _context.Orders
                .Include(x => x.Items)
                .Include(x => x.DiningTable)
                .SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken);

            if (order is null)
                throw new KeyNotFoundException("Pending order not found.");

            if (!order.IsActive)
                throw new InvalidOperationException("This pending order is inactive and cannot be paid.");

            if (order.BillStatus != OrderStatus.Pending)
                throw new InvalidOperationException("This order is no longer pending and cannot be paid again.");

            if (!order.Items.Any(x => x.Quantity > x.CancelledQuantity))
                throw new InvalidOperationException("The pending order has no items to pay for.");

            var kitchenTicket = await _context.KitchenOrderTickets
                .Where(x => x.OrderId == orderId)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (kitchenTicket is null)
                throw new InvalidOperationException("The pending order does not have a kitchen ticket.");

            var hasExistingPayment = await _context.Payments
                .AsNoTracking()
                .AnyAsync(x => x.OrderId == orderId, cancellationToken);

            if (hasExistingPayment)
                throw new InvalidOperationException("This order already has a payment record.");

            var requestedDiscount = dto.Discount ?? order.Discount;
            if (requestedDiscount < 0)
                throw new ArgumentException("Discount cannot be negative.");

            if (requestedDiscount >= order.SubTotal && requestedDiscount > 0)
                throw new ArgumentException("Discount must be less than the subtotal.");

            var discountChanged = requestedDiscount != order.Discount;
            var hasMatchingDiscountAudit = requestedDiscount > 0 &&
                await _context.DiscountAudits
                    .AsNoTracking()
                    .AnyAsync(
                        x => x.OrderId == order.Id &&
                             x.ApprovedDiscount == requestedDiscount,
                        cancellationToken);

            if (requestedDiscount > 0 &&
                (discountChanged || !hasMatchingDiscountAudit))
            {
                var previousDiscount = order.Discount;

                var approval = dto.DiscountApproval
                    ?? throw new UnauthorizedAccessException(
                        "Valid active Admin approval is required for a non-zero discount.");

                var approvedByUserId = await _authService.ValidateActiveAdminAsync(
                    approval,
                    cancellationToken);

                _context.DiscountAudits.Add(new DiscountAudit
                {
                    OrderId = order.Id,
                    SubTotal = order.SubTotal,
                    PreviousDiscount = previousDiscount,
                    ApprovedDiscount = requestedDiscount,
                    GrandTotal = order.SubTotal - requestedDiscount + order.Tax,
                    Reason = approval.Reason.Trim(),
                    RequestedByUserId = authenticatedUserId,
                    ApprovedByUserId = approvedByUserId,
                    CreatedOn = DateTime.UtcNow
                });
            }

            if (discountChanged)
            {
                order.Discount = requestedDiscount;
                order.GrandTotal = order.SubTotal - order.Discount + order.Tax;
            }

            var paymentLines = BuildPaymentLines(dto.Payments, order.GrandTotal);
            var paymentMethod = paymentLines.Count == 1
                ? paymentLines[0].PaymentMethod
                : PaymentMode.Mixed;
            var paymentDate = DateTime.Now;

            var payment = new Payment
            {
                OrderId = order.Id,
                BillAmount = order.SubTotal,
                DiscountAmount = order.Discount,
                TaxAmount = order.Tax,
                GrandTotal = order.GrandTotal,
                PaymentMethod = paymentMethod,
                PaymentStatus = PaymentStatus.Paid,
                PaidAmount = order.GrandTotal,
                BalanceAmount = 0m,
                TransactionNumber = paymentLines.Count == 1
                    ? paymentLines[0].TransactionNumber
                    : null,
                PaymentDate = paymentDate,
                UserId = authenticatedUserId,
                Allocations = paymentLines.Select(x => new PaymentAllocation
                {
                    PaymentMethod = x.PaymentMethod,
                    Amount = x.Amount,
                    TenderedAmount = x.TenderedAmount,
                    ChangeAmount = x.ChangeAmount,
                    TransactionNumber = x.TransactionNumber
                }).ToList()
            };

            order.BillStatus = OrderStatus.Paid;
            order.UpdatedOn = DateTime.UtcNow;

            if (order.OrderType == OrderType.DineIn)
            {
                var diningTable = order.DiningTable
                    ?? throw new InvalidOperationException("Dining table not found for this order.");

                if (!string.Equals(diningTable.Status, DiningTableStatus.PaymentPending, StringComparison.OrdinalIgnoreCase) ||
                    !order.BillPrintedOn.HasValue)
                    throw new InvalidOperationException(
                        $"Print the requested bill for table {diningTable.TableNumber} before collecting payment.");

                diningTable.Status = DiningTableStatus.CleaningPending;
                diningTable.UpdatedOn = DateTime.UtcNow;
            }

            _context.Payments.Add(payment);
            _context.CustomerBillPrintJobs.Add(new CustomerBillPrintJob
            {
                OrderId = order.Id,
                RequestedByUserId = authenticatedUserId,
                DocumentType = CustomerBillDocumentType.PaidReceipt,
                RequestKey = $"receipt:{order.BillNumber}",
                Status = CustomerBillPrintJobStatus.Pending
            });
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new CheckoutResponseDto
            {
                OrderId = order.Id,
                BillNumber = order.BillNumber,
                KitchenTicketNumber = kitchenTicket.KotNumber,
                BillDate = paymentDate,
                SubTotal = order.SubTotal,
                Discount = order.Discount,
                Tax = order.Tax,
                GrandTotal = order.GrandTotal,
                PaidAmount = order.GrandTotal,
                TenderedAmount = paymentLines.Sum(x => x.TenderedAmount),
                ChangeAmount = paymentLines.Sum(x => x.ChangeAmount),
                PaymentMethod = paymentMethod,
                Payments = paymentLines.Select(x => new CheckoutPaymentResponseDto
                {
                    PaymentMethod = x.PaymentMethod,
                    Amount = x.Amount,
                    TenderedAmount = x.TenderedAmount,
                    ChangeAmount = x.ChangeAmount,
                    TransactionNumber = x.TransactionNumber
                }).ToList()
            };
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new InvalidOperationException(
                "Pending-order checkout could not be saved. No payment was created.", ex);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<NextBillNumberPreviewDto> GetNextBillNumberPreviewAsync(
        CancellationToken cancellationToken = default)
    {
        return new NextBillNumberPreviewDto
        {
            BillNumber = await _billCounterRepository.GetNextBillNumberPreviewAsync(cancellationToken)
        };
    }

    private async Task<User> GetCashierAsync(
        int authenticatedUserId,
        CancellationToken cancellationToken)
    {
        return await _context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == authenticatedUserId && x.IsActive, cancellationToken)
            ?? throw new UnauthorizedAccessException("The signed-in cashier no longer exists.");
    }

    private async Task EnsureCashierExistsAsync(
        int authenticatedUserId,
        CancellationToken cancellationToken)
    {
        _ = await GetCashierAsync(authenticatedUserId, cancellationToken);
    }

    private async Task<KitchenOrderTicket?> GetFirstKitchenTicketAsync(
        int orderId,
        CancellationToken cancellationToken)
    {
        return await _context.KitchenOrderTickets
            .AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static void EnsureBillCanBePrinted(Order order)
    {
        EnsureOpenDineInOrder(order);
        var status = order.DiningTable!.Status;

        if (!string.Equals(status, DiningTableStatus.BillRequested, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(status, DiningTableStatus.PaymentPending, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The Captain has not requested this table bill.");
        }
    }

    private static void EnsureOpenDineInOrder(Order order)
    {
        if (!order.IsActive || order.BillStatus != OrderStatus.Pending || order.OrderType != OrderType.DineIn)
            throw new InvalidOperationException("This table order is no longer pending.");

        if (order.DiningTable is null)
            throw new InvalidOperationException("Dining table not found for this order.");
    }

    private async Task<DiningTable?> ResolveDiningTableAsync(
        OrderType orderType,
        int? diningTableId,
        CancellationToken cancellationToken)
    {
        if (orderType != OrderType.DineIn)
            return null;

        if (!diningTableId.HasValue)
            throw new ArgumentException("Dining table is required for dine-in orders.");

        var diningTable = await _context.DiningTables
            .SingleOrDefaultAsync(x => x.Id == diningTableId.Value && x.IsActive, cancellationToken);

        if (diningTable is null)
            throw new ArgumentException("Dining table not found.");

        if (!DiningTableStatus.IsAvailable(diningTable.Status))
            throw new InvalidOperationException($"Table {diningTable.TableNumber} is not available.");

        return diningTable;
    }

    private async Task<string?> ResolveCustomerNameAsync(
        int? customerId,
        string? requestedCustomerName,
        CancellationToken cancellationToken)
    {
        var customerName = string.IsNullOrWhiteSpace(requestedCustomerName)
            ? null
            : requestedCustomerName.Trim();

        if (!customerId.HasValue)
            return customerName;

        var customer = await _context.Customers
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == customerId.Value, cancellationToken);

        if (customer is null)
            throw new ArgumentException("Customer not found.");

        return customerName ?? customer.Name;
    }

private async Task<Dictionary<int, MenuItem>> GetMenuItemsAsync(
    IEnumerable<int> requestedItemIds,
    CancellationToken cancellationToken)
{
    var requestedMenuItemIds = requestedItemIds.Distinct().ToList();
    var menuItems = await _context.MenuItems
        .Where(x => requestedMenuItemIds.Contains(x.Id))
        .ToDictionaryAsync(x => x.Id, cancellationToken);

    if (menuItems.Count != requestedMenuItemIds.Count)
        throw new ArgumentException("One or more menu items were not found.");

    return menuItems;
}

    private static decimal GetItemPrice(
        MenuItem menuItem,
        OrderType orderType,
        DiningTable? diningTable)
    {
        if (orderType == OrderType.Parcel ||
            orderType == OrderType.HomeDelivery)
        {
            return menuItem.ParcelPrice;
        }

        if (diningTable is null)
        {
            throw new InvalidOperationException(
                "Dining table is required for dine-in pricing.");
        }

        return string.Equals(
            diningTable.TableType,
            "AC",
            StringComparison.OrdinalIgnoreCase)
            ? menuItem.ACPrice
            : menuItem.NonACPrice;
    }

    private static PendingOrderDto MapPendingOrder(Order order, KitchenOrderTicket? kitchenTicket)
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
        KitchenTicketNumber = kitchenTicket?.KotNumber ?? string.Empty,
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

    private static void ValidateCreateRequest(CreatePendingOrderDto dto)
    {
        if (!Enum.IsDefined(typeof(OrderType), dto.OrderType))
            throw new ArgumentException("Invalid order type.");

        if (dto.Items is null || dto.Items.Count == 0)
            throw new ArgumentException("At least one order item is required.");

        if (dto.Discount < 0 || dto.Tax < 0)
            throw new ArgumentException("Discount and tax cannot be negative.");

        if (dto.CustomerId.HasValue && dto.CustomerId.Value <= 0)
            throw new ArgumentException("Invalid customer.");

        if (dto.CustomerName?.Length > 150)
            throw new ArgumentException("Customer name cannot exceed 150 characters.");

        if (dto.Remarks?.Length > 500)
            throw new ArgumentException("Remarks cannot exceed 500 characters.");

        foreach (var item in dto.Items)
        {
            if (item.MenuItemId <= 0 || item.Quantity is < 1 or > 100)
                throw new ArgumentException("Each order item must have a valid menu item and quantity.");
        }
    }

    private static void ValidateCheckoutRequest(PendingOrderCheckoutDto dto)
    {
        if (dto.Payments is null || dto.Payments.Count is < 1 or > 2)
            throw new ArgumentException("Provide one payment or two split-payment lines.");

        if (dto.Discount < 0)
            throw new ArgumentException("Discount cannot be negative.");
    }

    private static List<PaymentLine> BuildPaymentLines(
        IEnumerable<CheckoutPaymentDto> requestedPayments,
        decimal grandTotal)
    {
        var lines = new List<PaymentLine>();

        foreach (var requestedPayment in requestedPayments)
        {
            if (requestedPayment.PaymentMethod is not (PaymentMode.Cash or PaymentMode.Upi or PaymentMode.Card))
                throw new ArgumentException("Use Cash, UPI, or Card for each payment line.");

            if (requestedPayment.Amount <= 0)
                throw new ArgumentException("Each payment amount must be greater than zero.");

            var tenderedAmount = requestedPayment.TenderedAmount ?? requestedPayment.Amount;

            if (tenderedAmount < requestedPayment.Amount)
                throw new ArgumentException("Tendered cash cannot be less than its payment amount.");

            if (requestedPayment.PaymentMethod != PaymentMode.Cash && tenderedAmount != requestedPayment.Amount)
                throw new ArgumentException("Only cash payments can have a tendered amount different from the payment amount.");

            lines.Add(new PaymentLine(
                requestedPayment.PaymentMethod,
                requestedPayment.Amount,
                tenderedAmount,
                tenderedAmount - requestedPayment.Amount,
                requestedPayment.TransactionNumber));
        }

        if (decimal.Abs(lines.Sum(x => x.Amount) - grandTotal) > 0.01m)
            throw new ArgumentException("Payment amounts must exactly match the bill total.");

        return lines;
    }

    private sealed record PaymentLine(
        PaymentMode PaymentMethod,
        decimal Amount,
        decimal TenderedAmount,
        decimal ChangeAmount,
        string? TransactionNumber);

    private static string ResolveReceiptOrderMode(Order order)
    {
        if (order.OrderType == OrderType.DineIn)
            return "DINE IN";

        if (order.OrderType == OrderType.HomeDelivery)
            return "HOME DELIVERY";

        if (order.OrderType == OrderType.Parcel &&
            string.Equals(
                order.Remarks?.Trim(),
                "Phone order",
                StringComparison.OrdinalIgnoreCase))
        {
            return "PHONE ORDER";
        }

        return "TAKE AWAY";
    }
}
