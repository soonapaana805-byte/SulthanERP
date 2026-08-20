using Microsoft.EntityFrameworkCore;
using Sulthan.Core.Common;
using Sulthan.Core.DTOs.Checkout;
using Sulthan.Core.Entities;
using Sulthan.Core.Enums;
using Sulthan.Core.Interfaces;
using Sulthan.Infrastructure.Data;
using System.Data;

namespace Sulthan.Infrastructure.Services;

public sealed class CheckoutService : ICheckoutService
{
    private readonly RestaurantDbContext _context;
    private readonly IBillCounterRepository _billCounterRepository;
    private readonly IAuthService _authService;

    public CheckoutService(
        RestaurantDbContext context,
        IBillCounterRepository billCounterRepository,
        IAuthService authService)
    {
        _context = context;
        _billCounterRepository = billCounterRepository;
        _authService = authService;
    }

    public async Task<CheckoutResponseDto> CheckoutAsync(
        CreateCheckoutDto dto,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(dto);

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        try
        {
            var cashierExists = await _context.Users
                .AsNoTracking()
                .AnyAsync(
                    x => x.Id == authenticatedUserId,
                    cancellationToken);

            if (!cashierExists)
            {
                throw new UnauthorizedAccessException(
                    "The signed-in cashier no longer exists.");
            }

            DiningTable? diningTable = null;

            if (dto.OrderType == OrderType.DineIn)
            {
                if (!dto.DiningTableId.HasValue)
                {
                    throw new ArgumentException(
                        "Dining table is required for dine-in orders.");
                }

                diningTable = await _context.DiningTables
                    .SingleOrDefaultAsync(
                        x => x.Id == dto.DiningTableId.Value,
                        cancellationToken);

                if (diningTable is null)
                {
                    throw new ArgumentException(
                        "Dining table not found.");
                }

                if (!DiningTableStatus.IsAvailable(diningTable.Status))
                {
                    throw new InvalidOperationException(
                        $"Table {diningTable.TableNumber} is not available.");
                }
            }

            var customerName = await ResolveCustomerNameAsync(
                dto.CustomerId,
                dto.CustomerName,
                cancellationToken);

            if (dto.OrderType == OrderType.HomeDelivery &&
                string.IsNullOrWhiteSpace(customerName))
            {
                throw new ArgumentException(
                    "Customer name is required for home delivery.");
            }

            var requestedMenuItemIds = dto.Items
                .Select(x => x.MenuItemId)
                .Distinct()
                .ToList();

            var menuItems = await _context.MenuItems
                .Where(x => requestedMenuItemIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);

            if (menuItems.Count != requestedMenuItemIds.Count)
            {
                throw new ArgumentException(
                    "One or more menu items were not found.");
            }

            var order = new Order
            {
                BillNumber =
                    await _billCounterRepository.GetNextBillNumberAsync(),

                OrderType = dto.OrderType,
                BillStatus = OrderStatus.Paid,

                DiningTableId = dto.OrderType == OrderType.DineIn
                    ? dto.DiningTableId
                    : null,

                CustomerId = dto.CustomerId,
                CustomerName = customerName,
                UserId = authenticatedUserId,
                Discount = dto.Discount,
                Tax = dto.Tax
            };

            foreach (var requestedItem in dto.Items)
            {
                var menuItem =
                    menuItems[requestedItem.MenuItemId];

                if (!menuItem.IsAvailable)
                {
                    throw new InvalidOperationException(
                        $"{menuItem.Name} is currently unavailable.");
                }

                var isParcelType =
                    dto.OrderType == OrderType.Parcel ||
                    dto.OrderType == OrderType.HomeDelivery;

                if (isParcelType && !menuItem.IsParcelAvailable)
                {
                    throw new InvalidOperationException(
                        $"{menuItem.Name} is not available for take away or home delivery.");
                }

                decimal price;

                if (isParcelType)
                {
                    price = menuItem.ParcelPrice;
                }
                else if (diningTable is not null &&
                         string.Equals(
                             diningTable.TableType,
                             "AC",
                             StringComparison.OrdinalIgnoreCase))
                {
                    price = menuItem.ACPrice;
                }
                else
                {
                    price = menuItem.NonACPrice;
                }

                order.Items.Add(new OrderItem
                {
                    MenuItemId = menuItem.Id,
                    Quantity = requestedItem.Quantity,
                    Price = price,
                    Notes = requestedItem.Notes
                });
            }

            order.SubTotal =
                order.Items.Sum(x => x.Price * x.Quantity);

            if (dto.Discount >= order.SubTotal && dto.Discount > 0)
            {
                throw new ArgumentException(
                    "Discount must be less than the subtotal.");
            }

            order.GrandTotal =
                order.SubTotal - order.Discount + order.Tax;

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

            var paymentLines =
                BuildPaymentLines(dto.Payments, order.GrandTotal);

            var totalApplied =
                paymentLines.Sum(x => x.Amount);

            if (decimal.Abs(
                    totalApplied - order.GrandTotal) > 0.01m)
            {
                throw new ArgumentException(
                    "Payment amounts must exactly match the bill total.");
            }

            var paymentMethod =
                paymentLines.Count == 1
                    ? paymentLines[0].PaymentMethod
                    : PaymentMode.Mixed;

            var paymentDate = DateTime.Now;

            var payment = new Payment
            {
                Order = order,
                BillAmount = order.SubTotal,
                DiscountAmount = order.Discount,
                TaxAmount = order.Tax,
                GrandTotal = order.GrandTotal,
                PaymentMethod = paymentMethod,
                PaymentStatus = PaymentStatus.Paid,
                PaidAmount = order.GrandTotal,
                BalanceAmount = 0m,

                TransactionNumber =
                    paymentLines.Count == 1
                        ? paymentLines[0].TransactionNumber
                        : null,

                PaymentDate = paymentDate,
                UserId = authenticatedUserId,

                Allocations = paymentLines
                    .Select(x => new PaymentAllocation
                    {
                        PaymentMethod = x.PaymentMethod,
                        Amount = x.Amount,
                        TenderedAmount = x.TenderedAmount,
                        ChangeAmount = x.ChangeAmount,
                        TransactionNumber =
                            x.TransactionNumber
                    })
                    .ToList()
            };

            var kitchenTicket = new KitchenOrderTicket
            {
                KotNumber = $"KOT-{order.BillNumber}",
                Order = order,
                PrintedOn = paymentDate,
                IsReprint = false,

                Items = order.Items
                    .Select(x => new KitchenOrderTicketItem
                    {
                        OrderItem = x,
                        MenuItemId = x.MenuItemId,
                        Quantity = x.Quantity,
                        Notes = x.Notes
                    })
                    .ToList()
            };

            _context.Orders.Add(order);
            _context.Payments.Add(payment);
            _context.KitchenOrderTickets.Add(kitchenTicket);

            _context.CustomerBillPrintJobs.Add(
                new CustomerBillPrintJob
                {
                    Order = order,
                    RequestedByUserId = authenticatedUserId,
                    DocumentType =
                        CustomerBillDocumentType.PaidReceipt,
                    RequestKey =
                        $"receipt:{order.BillNumber}",
                    Status =
                        CustomerBillPrintJobStatus.Pending
                });

            if (diningTable is not null)
            {
                diningTable.Status =
                    DiningTableStatus.CleaningPending;

                diningTable.UpdatedOn = DateTime.UtcNow;
            }

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

                TenderedAmount =
                    paymentLines.Sum(x => x.TenderedAmount),

                ChangeAmount =
                    paymentLines.Sum(x => x.ChangeAmount),

                PaymentMethod = paymentMethod,

                Payments = paymentLines
                    .Select(x =>
                        new CheckoutPaymentResponseDto
                        {
                            PaymentMethod =
                                x.PaymentMethod,

                            Amount = x.Amount,

                            TenderedAmount =
                                x.TenderedAmount,

                            ChangeAmount =
                                x.ChangeAmount,

                            TransactionNumber =
                                x.TransactionNumber
                        })
                    .ToList()
            };
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            throw new InvalidOperationException(
                "Checkout could not be saved. " +
                "No order or payment was created.",
                ex);
        }
        catch
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            throw;
        }
    }

    private static void ValidateRequest(
        CreateCheckoutDto dto)
    {
        if (!Enum.IsDefined(
                typeof(OrderType),
                dto.OrderType))
        {
            throw new ArgumentException(
                "Invalid order type.");
        }

        if (dto.Items.Count == 0)
        {
            throw new ArgumentException(
                "At least one order item is required.");
        }

        if (dto.Payments.Count is < 1 or > 2)
        {
            throw new ArgumentException(
                "Provide one payment or two split-payment lines.");
        }

        if (dto.Discount < 0 || dto.Tax < 0)
        {
            throw new ArgumentException(
                "Discount and tax cannot be negative.");
        }

        if (dto.CustomerId.HasValue &&
            dto.CustomerId.Value <= 0)
        {
            throw new ArgumentException(
                "Invalid customer.");
        }

        if (dto.CustomerName?.Length > 150)
        {
            throw new ArgumentException(
                "Customer name cannot exceed 150 characters.");
        }

        foreach (var item in dto.Items)
        {
            if (item.MenuItemId <= 0 ||
                item.Quantity is < 1 or > 100)
            {
                throw new ArgumentException(
                    "Each order item must have a valid menu item and quantity.");
            }
        }
    }

    private static List<PaymentLine> BuildPaymentLines(
        IEnumerable<CheckoutPaymentDto> requestedPayments,
        decimal grandTotal)
    {
        var lines = new List<PaymentLine>();

        foreach (var requestedPayment in requestedPayments)
        {
            if (requestedPayment.PaymentMethod is not
                (PaymentMode.Cash or
                 PaymentMode.Upi or
                 PaymentMode.Card))
            {
                throw new ArgumentException(
                    "Use Cash, UPI, or Card for each payment line.");
            }

            if (requestedPayment.Amount <= 0)
            {
                throw new ArgumentException(
                    "Each payment amount must be greater than zero.");
            }

            var tenderedAmount =
                requestedPayment.TenderedAmount ??
                requestedPayment.Amount;

            if (tenderedAmount < requestedPayment.Amount)
            {
                throw new ArgumentException(
                    "Tendered cash cannot be less than its payment amount.");
            }

            if (requestedPayment.PaymentMethod != PaymentMode.Cash &&
                tenderedAmount != requestedPayment.Amount)
            {
                throw new ArgumentException(
                    "Only cash payments can have a tendered amount different from the payment amount.");
            }

            lines.Add(new PaymentLine(
                requestedPayment.PaymentMethod,
                requestedPayment.Amount,
                tenderedAmount,
                tenderedAmount - requestedPayment.Amount,
                requestedPayment.TransactionNumber));
        }

        if (decimal.Abs(
                lines.Sum(x => x.Amount) -
                grandTotal) > 0.01m)
        {
            throw new ArgumentException(
                "Payment amounts must exactly match the bill total.");
        }

        return lines;
    }

    private async Task<string?> ResolveCustomerNameAsync(
        int? customerId,
        string? requestedCustomerName,
        CancellationToken cancellationToken)
    {
        var customerName =
            string.IsNullOrWhiteSpace(requestedCustomerName)
                ? null
                : requestedCustomerName.Trim();

        if (!customerId.HasValue)
            return customerName;

        var customer = await _context.Customers
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == customerId.Value,
                cancellationToken);

        if (customer is null)
        {
            throw new ArgumentException(
                "Customer not found.");
        }

        return customerName ?? customer.Name;
    }

    private sealed record PaymentLine(
        PaymentMode PaymentMethod,
        decimal Amount,
        decimal TenderedAmount,
        decimal ChangeAmount,
        string? TransactionNumber);
}
