using Microsoft.EntityFrameworkCore;
using Sulthan.Core.Common;
using Sulthan.Core.DTOs.Auth;
using Sulthan.Core.DTOs.Billing;
using Sulthan.Core.DTOs.KitchenOrders;
using Sulthan.Core.Entities;
using Sulthan.Core.Enums;
using Sulthan.Core.Interfaces;
using Sulthan.Infrastructure.Data;
using System.Data;

namespace Sulthan.Infrastructure.Services;

public class BillingService : IBillingService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IReceiptFormatter _receiptFormatter;
    private readonly RestaurantDbContext _context;
    private readonly IAuthService _authService;

    public BillingService(
        IOrderRepository orderRepository,
        IPaymentRepository paymentRepository,
        IReceiptFormatter receiptFormatter,
        RestaurantDbContext context,
        IAuthService authService)
    {
        _orderRepository = orderRepository;
        _paymentRepository = paymentRepository;
        _receiptFormatter = receiptFormatter;
        _context = context;
        _authService = authService;
    }

    public async Task<BillResponseDto?> GetBillAsync(
        int orderId)
    {
        var order =
            await _orderRepository.GetByIdAsync(orderId);

        if (order == null)
            return null;

        var payment =
            await _paymentRepository
                .GetByOrderIdAsync(orderId);

        if (payment == null)
            return null;

        var paymentLines =
            payment.Allocations.Count > 0
                ? payment.Allocations
                    .OrderBy(x => x.Id)
                    .Select(x => new BillPaymentDto
                    {
                        PaymentMethod =
                            x.PaymentMethod.ToString(),
                        PaidAmount = x.Amount,
                        TenderedAmount = x.TenderedAmount,
                        ChangeAmount = x.ChangeAmount,
                        TransactionNumber =
                            x.TransactionNumber,
                        PaymentDate = payment.PaymentDate
                    })
                    .ToList()
                : new List<BillPaymentDto>
                {
                    new()
                    {
                        PaymentMethod =
                            payment.PaymentMethod.ToString(),
                        PaidAmount = payment.PaidAmount,
                        TenderedAmount = payment.PaidAmount,
                        ChangeAmount = 0m,
                        TransactionNumber =
                            payment.TransactionNumber,
                        PaymentDate = payment.PaymentDate
                    }
                };

        var bill = new BillResponseDto
        {
            BillNumber = order.BillNumber,
            OrderType = ResolveReceiptOrderMode(order),
            TableNumber =
                order.DiningTable?.TableNumber,
            CustomerName =
                order.CustomerName ?? order.Customer?.Name,
            CaptainName =
                order.User?.FullName ?? string.Empty,
            CashierName =
                payment.User?.FullName ?? string.Empty,
            BillDate = payment.PaymentDate,

            SubTotal = order.SubTotal,
            Discount = order.Discount,
            Tax = order.Tax,
            GrandTotal = order.GrandTotal,

            PaidAmount = payment.PaidAmount,
            BalanceAmount = payment.BalanceAmount,
            PaymentMethod =
                payment.PaymentMethod.ToString(),
            Payments = paymentLines
        };

        foreach (var item in order.Items.Where(x =>
                     x.Quantity > x.CancelledQuantity))
        {
            var activeQuantity = item.Quantity - item.CancelledQuantity;
            bill.Items.Add(new BillItemDto
            {
                MenuItemId = item.MenuItemId,
                ItemName =
                    item.MenuItem?.Name ?? string.Empty,
                Price = item.Price,
                Quantity = activeQuantity,
                Total = item.Price * activeQuantity
            });
        }

        return bill;
    }

    public async Task<BillResponseDto?> ReprintBillAsync(
        string billNumber)
    {
        var orders =
            await _orderRepository.GetAllAsync();

        var order = orders.FirstOrDefault(
            x => x.BillNumber == billNumber);

        if (order == null)
            return null;

        if (order.BillStatus == OrderStatus.Voided)
            throw new InvalidOperationException("Voided receipts cannot be reprinted.");

        return await GetBillAsync(order.Id);
    }

    public async Task<string?> PrintBillAsync(
        string billNumber)
    {
        var bill =
            await ReprintBillAsync(billNumber);

        if (bill == null)
            return null;

        return await _receiptFormatter
            .GenerateReceiptAsync(
                new PrintBillDto
                {
                    DocumentTitle = "CUSTOMER RECEIPT",
                    BillNumber = bill.BillNumber,
                    TableNumber =
                        bill.TableNumber ?? string.Empty,
                    CustomerName = bill.CustomerName,
                    CaptainName = bill.CaptainName,
                    CashierName = bill.CashierName,
                    OrderType = bill.OrderType,
                    BillDate = bill.BillDate,
                    SubTotal = bill.SubTotal,
                    Discount = bill.Discount,
                    Tax = bill.Tax,
                    GrandTotal = bill.GrandTotal,
                    PaidAmount = bill.PaidAmount,
                    BalanceAmount = bill.BalanceAmount,
                    PaymentMethod = bill.PaymentMethod,
                    Payments = bill.Payments,
                    Items = bill.Items
                });
    }

    public async Task<bool> QueueReceiptReprintAsync(
        string billNumber,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.BillNumber == billNumber,
                cancellationToken);

        if (order == null)
            return false;

        if (order.BillStatus == OrderStatus.Voided)
            throw new InvalidOperationException("Voided receipts cannot be reprinted.");

        if (order.BillStatus != OrderStatus.Paid)
            throw new InvalidOperationException("Only a paid receipt can be reprinted.");

        var paymentExists = await _context.Payments
            .AsNoTracking()
            .AnyAsync(
                x => x.OrderId == order.Id,
                cancellationToken);

        if (!paymentExists)
        {
            throw new InvalidOperationException(
                "Only a paid receipt can be reprinted " +
                "from billing history.");
        }

        _context.CustomerBillPrintJobs.Add(
            new CustomerBillPrintJob
            {
                OrderId = order.Id,
                RequestedByUserId = authenticatedUserId,
                DocumentType =
                    CustomerBillDocumentType.PaidReceipt,
                IsReprint = true,
                RequestKey =
                    $"receipt-reprint:{order.BillNumber}:" +
                    $"{Guid.NewGuid():N}",
                Status =
                    CustomerBillPrintJobStatus.Pending
            });

        await _context.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    public async Task<BillLifecycleDto?> GetBillLifecycleAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await LoadLifecycleOrderAsync(orderId, cancellationToken);
        return order is null ? null : await MapLifecycleAsync(order, cancellationToken);
    }

    public Task<BillLifecycleDto> CancelBillAsync(
        int orderId,
        ManagerApprovalDto approval,
        int authenticatedUserId,
        CancellationToken cancellationToken = default) =>
        ReverseBillAsync(
            orderId,
            approval,
            authenticatedUserId,
            BillActionType.Cancel,
            cancellationToken);

    public Task<BillLifecycleDto> VoidBillAsync(
        int orderId,
        ManagerApprovalDto approval,
        int authenticatedUserId,
        CancellationToken cancellationToken = default) =>
        ReverseBillAsync(
            orderId,
            approval,
            authenticatedUserId,
            BillActionType.Void,
            cancellationToken);

    public async Task<IReadOnlyList<DiscountAuditDto>> GetDiscountAuditsAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
            throw new ArgumentException("From date cannot be later than to date.");

        var query = _context.DiscountAudits
            .AsNoTracking()
            .Include(x => x.Order)
            .Include(x => x.RequestedByUser)
            .Include(x => x.ApprovedByUser)
            .AsQueryable();

        var configuredTimeZoneId = await _context.Settings
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Id)
            .Select(x => x.TimeZone)
            .FirstOrDefaultAsync(cancellationToken);

        var businessTimeZone = ResolveBusinessTimeZone(configuredTimeZoneId);

        if (fromDate.HasValue)
        {
            var fromUtc = ConvertBusinessDateToUtc(
                fromDate.Value,
                businessTimeZone);
            query = query.Where(x => x.CreatedOn >= fromUtc);
        }

        if (toDate.HasValue)
        {
            var toExclusiveUtc = ConvertBusinessDateToUtc(
                toDate.Value.AddDays(1),
                businessTimeZone);
            query = query.Where(x => x.CreatedOn < toExclusiveUtc);
        }

        return await query
            .OrderByDescending(x => x.CreatedOn)
            .ThenByDescending(x => x.Id)
            .Select(x => new DiscountAuditDto
            {
                Id = x.Id,
                OrderId = x.OrderId,
                BillNumber = x.Order.BillNumber,
                SubTotal = x.SubTotal,
                PreviousDiscount = x.PreviousDiscount,
                ApprovedDiscount = x.ApprovedDiscount,
                GrandTotal = x.GrandTotal,
                Reason = x.Reason,
                RequestedByUserId = x.RequestedByUserId,
                RequestedByUserName = x.RequestedByUser.FullName,
                ApprovedByUserId = x.ApprovedByUserId,
                ApprovedByUserName = x.ApprovedByUser.FullName,
                ApprovedOn = x.CreatedOn
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BillActionAuditDto>> GetBillActionAuditsAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
            throw new ArgumentException("From date cannot be later than to date.");

        var query = _context.BillActionAudits
            .AsNoTracking()
            .Include(x => x.RequestedByUser)
            .Include(x => x.ApprovedByUser)
            .AsQueryable();
        var configuredTimeZoneId = await _context.Settings
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Id)
            .Select(x => x.TimeZone)
            .FirstOrDefaultAsync(cancellationToken);
        var businessTimeZone = ResolveBusinessTimeZone(configuredTimeZoneId);

        if (fromDate.HasValue)
        {
            var fromUtc = ConvertBusinessDateToUtc(fromDate.Value, businessTimeZone);
            query = query.Where(x => x.ActionOn >= fromUtc);
        }

        if (toDate.HasValue)
        {
            var toExclusiveUtc = ConvertBusinessDateToUtc(toDate.Value.AddDays(1), businessTimeZone);
            query = query.Where(x => x.ActionOn < toExclusiveUtc);
        }

        return await query
            .OrderByDescending(x => x.ActionOn)
            .ThenByDescending(x => x.Id)
            .Select(x => new BillActionAuditDto
            {
                Id = x.Id,
                OrderId = x.OrderId,
                BillNumber = x.BillNumber,
                ActionType = x.ActionType,
                Reason = x.Reason,
                RequestedByUserId = x.RequestedByUserId,
                RequestedByUserName = x.RequestedByUser.FullName,
                ApprovedByUserId = x.ApprovedByUserId,
                ApprovedByUserName = x.ApprovedByUser.FullName,
                ActionOn = x.ActionOn,
                PreviousOrderStatus = x.PreviousOrderStatus,
                NewOrderStatus = x.NewOrderStatus,
                PreviousPaymentStatus = x.PreviousPaymentStatus,
                NewPaymentStatus = x.NewPaymentStatus,
                FinancialAmount = x.FinancialAmount,
                PreviousTableStatus = x.PreviousTableStatus,
                NewTableStatus = x.NewTableStatus
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<KotCancellationResultDto> CancelKotAsync(
        int kitchenOrderTicketId,
        ManagerApprovalDto approval,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateActionApproval(approval, BillActionType.Cancel);
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var requester = await _context.Users.SingleOrDefaultAsync(
                x => x.Id == authenticatedUserId && x.IsActive,
                cancellationToken)
                ?? throw new UnauthorizedAccessException(
                    "The signed-in user is no longer active.");

            var ticket = await _context.KitchenOrderTickets
                .Include(x => x.Order!)
                    .ThenInclude(x => x.Items)
                .Include(x => x.Items)
                    .ThenInclude(x => x.MenuItem)
                .Include(x => x.Items)
                    .ThenInclude(x => x.OrderItem)
                .SingleOrDefaultAsync(
                    x => x.Id == kitchenOrderTicketId,
                    cancellationToken)
                ?? throw new KeyNotFoundException("KOT not found.");
            var order = ticket.Order
                ?? throw new InvalidOperationException("The KOT has no bill.");

            if (!string.Equals(
                    ticket.Status,
                    KitchenOrderTicketStatus.Active,
                    StringComparison.OrdinalIgnoreCase) ||
                await _context.KotCancellationAudits.AsNoTracking().AnyAsync(
                    x => x.KitchenOrderTicketId == ticket.Id,
                    cancellationToken))
            {
                throw new InvalidOperationException(
                    "This KOT has already been cancelled.");
            }

            var paymentExists = await _context.Payments.AsNoTracking().AnyAsync(
                x => x.OrderId == order.Id,
                cancellationToken);
            if (!IsUnpaidOperationalStatus(order.BillStatus) || paymentExists)
            {
                throw new InvalidOperationException(
                    "Only a KOT from an unpaid bill with no payment record can be cancelled.");
            }

            if (ticket.Items.Count == 0 || ticket.Items.Any(x =>
                    !x.OrderItemId.HasValue ||
                    x.OrderItem is null ||
                    x.OrderItem.OrderId != order.Id ||
                    x.OrderItem.MenuItemId != x.MenuItemId))
            {
                throw new InvalidOperationException(
                    "This KOT was created before item ownership tracking and cannot be cancelled separately. Use Cancel entire bill.");
            }

            foreach (var item in ticket.Items)
            {
                if (item.Quantity != decimal.Truncate(item.Quantity) ||
                    item.Quantity <= 0 ||
                    item.CancelledQuantity > 0 ||
                    item.OrderItem!.CancelledQuantity + item.Quantity >
                        item.OrderItem.Quantity)
                {
                    throw new InvalidOperationException(
                        "This KOT cannot be cancelled because its item quantities are inconsistent.");
                }
            }

            var approvedByUserId = await _authService.ValidateActiveAdminAsync(
                approval,
                cancellationToken);
            var approver = await _context.Users.AsNoTracking().SingleAsync(
                x => x.Id == approvedByUserId,
                cancellationToken);
            var cancelledOn = DateTime.UtcNow;
            var previousSubTotal = order.SubTotal;
            var previousDiscount = order.Discount;
            var previousTax = order.Tax;
            var previousGrandTotal = order.GrandTotal;

            foreach (var item in ticket.Items)
            {
                item.CancelledQuantity = item.Quantity;
                item.UpdatedOn = cancelledOn;
                item.OrderItem!.CancelledQuantity += (int)item.Quantity;
                item.OrderItem.UpdatedOn = cancelledOn;
            }

            ticket.Status = KitchenOrderTicketStatus.Cancelled;
            ticket.CancelledOn = cancelledOn;
            ticket.UpdatedOn = cancelledOn;
            order.SubTotal = order.Items.Sum(x =>
                x.Price * Math.Max(0, x.Quantity - x.CancelledQuantity));
            order.Discount = 0m;
            order.Tax = previousSubTotal <= 0m
                ? 0m
                : Math.Round(
                    previousTax * order.SubTotal / previousSubTotal,
                    2,
                    MidpointRounding.AwayFromZero);
            order.GrandTotal = order.SubTotal + order.Tax;
            order.UpdatedOn = cancelledOn;

            var audit = CreateKotCancellationAudit(
                ticket,
                order,
                KotCancellationSource.SelectedKot,
                approval.Reason.Trim(),
                requester,
                approver,
                cancelledOn,
                previousSubTotal,
                previousDiscount,
                previousTax,
                previousGrandTotal);
            _context.KotCancellationAudits.Add(audit);
            await QueueKotCancellationSlipsAsync(
                ticket,
                audit,
                cancelledOn,
                cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new KotCancellationResultDto
            {
                KitchenOrderTicketId = ticket.Id,
                OrderId = order.Id,
                KotNumber = ticket.KotNumber,
                BillNumber = order.BillNumber,
                Status = ticket.Status,
                SubTotal = order.SubTotal,
                Discount = order.Discount,
                Tax = order.Tax,
                GrandTotal = order.GrandTotal,
                CancelledOn = cancelledOn
            };
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new InvalidOperationException(
                "This KOT could not be cancelled. It may already have been processed.",
                exception);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<IReadOnlyList<KotCancellationAuditDto>>
        GetKotCancellationAuditsAsync(
            DateOnly? fromDate,
            DateOnly? toDate,
            CancellationToken cancellationToken = default)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
            throw new ArgumentException("From date cannot be later than to date.");

        var query = _context.KotCancellationAudits.AsNoTracking().AsQueryable();
        var configuredTimeZoneId = await _context.Settings.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Id)
            .Select(x => x.TimeZone)
            .FirstOrDefaultAsync(cancellationToken);
        var businessTimeZone = ResolveBusinessTimeZone(configuredTimeZoneId);

        if (fromDate.HasValue)
        {
            var fromUtc = ConvertBusinessDateToUtc(fromDate.Value, businessTimeZone);
            query = query.Where(x => x.CancelledOn >= fromUtc);
        }

        if (toDate.HasValue)
        {
            var toExclusiveUtc = ConvertBusinessDateToUtc(
                toDate.Value.AddDays(1),
                businessTimeZone);
            query = query.Where(x => x.CancelledOn < toExclusiveUtc);
        }

        return await query
            .OrderByDescending(x => x.CancelledOn)
            .ThenByDescending(x => x.Id)
            .Select(x => new KotCancellationAuditDto
            {
                Id = x.Id,
                KitchenOrderTicketId = x.KitchenOrderTicketId,
                OrderId = x.OrderId,
                KotNumber = x.KotNumber,
                BillNumber = x.BillNumber,
                Source = x.Source,
                Reason = x.Reason,
                RequestedByUserId = x.RequestedByUserId,
                RequestedByName = x.RequestedByName,
                ApprovedByUserId = x.ApprovedByUserId,
                ApprovedByName = x.ApprovedByName,
                CancelledOn = x.CancelledOn,
                PreviousStatus = x.PreviousStatus,
                NewStatus = x.NewStatus,
                PreviousSubTotal = x.PreviousSubTotal,
                PreviousDiscount = x.PreviousDiscount,
                PreviousTax = x.PreviousTax,
                PreviousGrandTotal = x.PreviousGrandTotal,
                NewSubTotal = x.NewSubTotal,
                NewDiscount = x.NewDiscount,
                NewTax = x.NewTax,
                NewGrandTotal = x.NewGrandTotal,
                Items = x.Items.Select(item => new KotCancellationAuditItemDto
                {
                    MenuItemId = item.MenuItemId,
                    ItemName = item.ItemName,
                    KitchenName = item.KitchenName,
                    CancelledQuantity = item.CancelledQuantity
                }).ToList()
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<BillLifecycleDto> ReverseBillAsync(
        int orderId,
        ManagerApprovalDto approval,
        int authenticatedUserId,
        BillActionType actionType,
        CancellationToken cancellationToken)
    {
        ValidateActionApproval(approval, actionType);
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var requester = await _context.Users
                .SingleOrDefaultAsync(
                    x => x.Id == authenticatedUserId && x.IsActive,
                    cancellationToken)
                ?? throw new UnauthorizedAccessException(
                    "The signed-in user is no longer active.");

            var order = await LoadLifecycleOrderAsync(orderId, cancellationToken)
                ?? throw new KeyNotFoundException("Bill not found.");
            var existingAction = await _context.BillActionAudits
                .AsNoTracking()
                .AnyAsync(x => x.OrderId == orderId, cancellationToken);
            if (existingAction || order.BillStatus is OrderStatus.Cancelled or OrderStatus.Voided)
                throw new InvalidOperationException("This bill has already been cancelled or voided.");

            var payment = await _context.Payments
                .Include(x => x.Allocations)
                .SingleOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
            ValidateActionEligibility(order, payment, actionType);
            var approvedByUserId = await _authService.ValidateActiveAdminAsync(
                approval,
                cancellationToken);
            var approver = await _context.Users.AsNoTracking().SingleAsync(
                x => x.Id == approvedByUserId,
                cancellationToken);

            var previousOrderStatus = order.BillStatus.ToString();
            var previousPaymentStatus = payment?.PaymentStatus.ToString();
            var previousTableStatus = order.DiningTable?.Status;
            var actionOn = DateTime.UtcNow;

            if (actionType == BillActionType.Cancel)
            {
                await CancelRemainingKotsForFullBillAsync(
                    order,
                    approval.Reason.Trim(),
                    requester,
                    approver,
                    actionOn,
                    cancellationToken);
                order.BillStatus = OrderStatus.Cancelled;
                await MoveOwnedDineInTableToCleaningAsync(order, actionOn, cancellationToken);
                var queuedBillJobs = await _context.CustomerBillPrintJobs
                    .Where(x => x.OrderId == order.Id &&
                        (x.Status == CustomerBillPrintJobStatus.Pending ||
                         x.Status == CustomerBillPrintJobStatus.Failed))
                    .ToListAsync(cancellationToken);
                foreach (var job in queuedBillJobs)
                {
                    job.Status = CustomerBillPrintJobStatus.Cancelled;
                    job.NextAttemptOn = null;
                    job.UpdatedOn = actionOn;
                }
            }
            else
            {
                order.BillStatus = OrderStatus.Voided;
                payment!.PaymentStatus = PaymentStatus.Voided;
                payment.UpdatedOn = actionOn;
            }

            order.UpdatedOn = actionOn;
            _context.BillActionAudits.Add(new BillActionAudit
            {
                OrderId = order.Id,
                BillNumber = order.BillNumber,
                ActionType = actionType,
                Reason = approval.Reason.Trim(),
                RequestedByUserId = authenticatedUserId,
                ApprovedByUserId = approvedByUserId,
                ActionOn = actionOn,
                PreviousOrderStatus = previousOrderStatus,
                NewOrderStatus = order.BillStatus.ToString(),
                PreviousPaymentStatus = previousPaymentStatus,
                NewPaymentStatus = payment?.PaymentStatus.ToString(),
                FinancialAmount = order.GrandTotal,
                PreviousTableStatus = previousTableStatus,
                NewTableStatus = order.DiningTable?.Status,
                CreatedOn = actionOn
            });

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return await MapLifecycleAsync(order, cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new InvalidOperationException(
                "This bill could not be cancelled or voided. It may already have been processed.", ex);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private KotCancellationAudit CreateKotCancellationAudit(
        KitchenOrderTicket ticket,
        Order order,
        KotCancellationSource source,
        string reason,
        User requester,
        User approver,
        DateTime cancelledOn,
        decimal previousSubTotal,
        decimal previousDiscount,
        decimal previousTax,
        decimal previousGrandTotal)
    {
        var audit = new KotCancellationAudit
        {
            KitchenOrderTicket = ticket,
            Order = order,
            KotNumber = ticket.KotNumber,
            BillNumber = order.BillNumber,
            Source = source,
            Reason = reason,
            RequestedByUserId = requester.Id,
            RequestedByName = requester.FullName,
            ApprovedByUserId = approver.Id,
            ApprovedByName = approver.FullName,
            CancelledOn = cancelledOn,
            PreviousStatus = KitchenOrderTicketStatus.Active,
            NewStatus = KitchenOrderTicketStatus.Cancelled,
            PreviousSubTotal = previousSubTotal,
            PreviousDiscount = previousDiscount,
            PreviousTax = previousTax,
            PreviousGrandTotal = previousGrandTotal,
            NewSubTotal = order.SubTotal,
            NewDiscount = order.Discount,
            NewTax = order.Tax,
            NewGrandTotal = order.GrandTotal,
            CreatedOn = cancelledOn
        };

        foreach (var item in ticket.Items)
        {
            var cancelledQuantity = source == KotCancellationSource.SelectedKot
                ? item.Quantity
                : item.Quantity - item.CancelledQuantity;
            if (cancelledQuantity <= 0)
                continue;

            audit.Items.Add(new KotCancellationAuditItem
            {
                MenuItemId = item.MenuItemId,
                ItemName = item.MenuItem?.Name ?? $"Item #{item.MenuItemId}",
                KitchenName = ResolveKitchenName(item.MenuItem?.KitchenName),
                CancelledQuantity = cancelledQuantity,
                CreatedOn = cancelledOn
            });
        }

        return audit;
    }

    private async Task CancelRemainingKotsForFullBillAsync(
        Order order,
        string reason,
        User requester,
        User approver,
        DateTime cancelledOn,
        CancellationToken cancellationToken)
    {
        var activeTickets = await _context.KitchenOrderTickets
            .Include(x => x.Items)
                .ThenInclude(x => x.MenuItem)
            .Include(x => x.Items)
                .ThenInclude(x => x.OrderItem)
            .Where(x =>
                x.OrderId == order.Id &&
                x.Status == KitchenOrderTicketStatus.Active)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        foreach (var ticket in activeTickets)
        {
            var audit = CreateKotCancellationAudit(
                ticket,
                order,
                KotCancellationSource.FullBill,
                reason,
                requester,
                approver,
                cancelledOn,
                order.SubTotal,
                order.Discount,
                order.Tax,
                order.GrandTotal);

            foreach (var item in ticket.Items)
            {
                var remainingQuantity = item.Quantity - item.CancelledQuantity;
                if (remainingQuantity > 0 &&
                    remainingQuantity == decimal.Truncate(remainingQuantity) &&
                    item.OrderItem is not null &&
                    item.OrderItem.OrderId == order.Id)
                {
                    item.OrderItem.CancelledQuantity = Math.Min(
                        item.OrderItem.Quantity,
                        item.OrderItem.CancelledQuantity + (int)remainingQuantity);
                    item.OrderItem.UpdatedOn = cancelledOn;
                }

                item.CancelledQuantity = item.Quantity;
                item.UpdatedOn = cancelledOn;
            }

            ticket.Status = KitchenOrderTicketStatus.Cancelled;
            ticket.CancelledOn = cancelledOn;
            ticket.UpdatedOn = cancelledOn;
            _context.KotCancellationAudits.Add(audit);
            await QueueKotCancellationSlipsAsync(
                ticket,
                audit,
                cancelledOn,
                cancellationToken);
        }
    }

    private async Task QueueKotCancellationSlipsAsync(
        KitchenOrderTicket ticket,
        KotCancellationAudit audit,
        DateTime cancelledOn,
        CancellationToken cancellationToken)
    {
        var originalJobs = await _context.KitchenPrintJobs
            .Where(x =>
                x.KitchenOrderTicketId == ticket.Id &&
                x.DocumentType == KitchenPrintDocumentType.OriginalKot)
            .ToListAsync(cancellationToken);
        var kitchenNames = originalJobs
            .Select(x => ResolveKitchenName(x.KitchenName))
            .Concat(audit.Items.Select(x => ResolveKitchenName(x.KitchenName)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (kitchenNames.Count == 0)
            kitchenNames.Add("Main Kitchen");

        foreach (var originalJob in originalJobs.Where(x =>
                     x.Status == KitchenPrintJobStatus.Pending ||
                     x.Status == KitchenPrintJobStatus.Failed))
        {
            originalJob.Status = KitchenPrintJobStatus.Cancelled;
            originalJob.NextAttemptOn = null;
            originalJob.UpdatedOn = cancelledOn;
        }

        foreach (var kitchenName in kitchenNames)
        {
            _context.KitchenPrintJobs.Add(new KitchenPrintJob
            {
                KitchenOrderTicket = ticket,
                KotCancellationAudit = audit,
                KitchenName = kitchenName,
                DocumentType = KitchenPrintDocumentType.KotCancellation,
                Status = KitchenPrintJobStatus.Pending,
                CreatedOn = cancelledOn
            });
        }
    }

    private static string ResolveKitchenName(string? kitchenName) =>
        string.IsNullOrWhiteSpace(kitchenName)
            ? "Main Kitchen"
            : kitchenName.Trim();

    private async Task<Order?> LoadLifecycleOrderAsync(
        int orderId,
        CancellationToken cancellationToken) =>
        await _context.Orders
            .Include(x => x.DiningTable)
            .Include(x => x.Customer)
            .Include(x => x.Items)
                .ThenInclude(x => x.MenuItem)
            .SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken);

    private async Task<BillLifecycleDto> MapLifecycleAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        var paymentStatus = await _context.Payments
            .AsNoTracking()
            .Where(x => x.OrderId == order.Id)
            .Select(x => (PaymentStatus?)x.PaymentStatus)
            .SingleOrDefaultAsync(cancellationToken);
        var kotNumber = await _context.KitchenOrderTickets
            .AsNoTracking()
            .Where(x => x.OrderId == order.Id)
            .OrderBy(x => x.Id)
            .Select(x => x.KotNumber)
            .FirstOrDefaultAsync(cancellationToken);

        return new BillLifecycleDto
        {
            OrderId = order.Id,
            BillNumber = order.BillNumber,
            KitchenTicketNumber = kotNumber,
            OrderType = order.OrderType,
            OrderStatus = order.BillStatus,
            PaymentStatus = paymentStatus,
            TableNumber = order.DiningTable?.TableNumber,
            TableStatus = order.DiningTable?.Status,
            CustomerName = order.CustomerName ?? order.Customer?.Name,
            GrandTotal = order.GrandTotal,
            CanCancel = IsUnpaidOperationalStatus(order.BillStatus) && paymentStatus is null,
            CanVoid = order.BillStatus == OrderStatus.Paid && paymentStatus == PaymentStatus.Paid,
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
        };
    }

    private async Task MoveOwnedDineInTableToCleaningAsync(
        Order order,
        DateTime actionOn,
        CancellationToken cancellationToken)
    {
        var table = order.DiningTable;
        if (order.OrderType != OrderType.DineIn || table is null)
            return;

        var statusBelongsToOpenOrder =
            string.Equals(table.Status, DiningTableStatus.Occupied, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(table.Status, DiningTableStatus.BillRequested, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(table.Status, DiningTableStatus.PaymentPending, StringComparison.OrdinalIgnoreCase);
        if (!statusBelongsToOpenOrder)
            return;

        var reassigned = await _context.Orders
            .AsNoTracking()
            .AnyAsync(
                x => x.Id != order.Id && x.IsActive &&
                     x.OrderType == OrderType.DineIn &&
                     x.DiningTableId == table.Id &&
                     x.BillStatus != OrderStatus.Cancelled &&
                     x.BillStatus != OrderStatus.Voided &&
                     x.BillStatus != OrderStatus.Paid,
                cancellationToken);
        if (reassigned)
            return;

        table.Status = DiningTableStatus.CleaningPending;
        table.UpdatedOn = actionOn;
    }

    private static void ValidateActionEligibility(
        Order order,
        Payment? payment,
        BillActionType actionType)
    {
        if (actionType == BillActionType.Cancel)
        {
            if (!IsUnpaidOperationalStatus(order.BillStatus) || payment is not null)
                throw new InvalidOperationException("Only an unpaid bill with no payment record can be cancelled.");
            return;
        }

        if (order.BillStatus != OrderStatus.Paid || payment?.PaymentStatus != PaymentStatus.Paid)
            throw new InvalidOperationException("Only a currently paid bill and payment can be voided.");
    }

    private static bool IsUnpaidOperationalStatus(OrderStatus status) =>
        status is OrderStatus.Pending or OrderStatus.Preparing or
            OrderStatus.Ready or OrderStatus.Served;

    private static void ValidateActionApproval(
        ManagerApprovalDto approval,
        BillActionType actionType)
    {
        ArgumentNullException.ThrowIfNull(approval);
        var reason = approval.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length < 3)
            throw new ArgumentException($"{actionType} reason is required and must contain at least 3 characters.");
        if (reason.Length > 250)
            throw new ArgumentException($"{actionType} reason cannot exceed 250 characters.");
    }

    private static TimeZoneInfo ResolveBusinessTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return TimeZoneInfo.Local;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Local;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Local;
        }
    }

    private static DateTime ConvertBusinessDateToUtc(
        DateOnly businessDate,
        TimeZoneInfo businessTimeZone)
    {
        var localMidnight = DateTime.SpecifyKind(
            businessDate.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Unspecified);

        return TimeZoneInfo.ConvertTimeToUtc(
            localMidnight,
            businessTimeZone);
    }

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
