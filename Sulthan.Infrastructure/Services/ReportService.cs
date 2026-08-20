using Sulthan.Core.DTOs.Reports;
using Microsoft.EntityFrameworkCore;
using Sulthan.Core.Entities;
using Sulthan.Core.Enums;
using Sulthan.Core.Interfaces;
using Sulthan.Infrastructure.Data;

namespace Sulthan.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ITableRepository _tableRepository;
    private readonly RestaurantDbContext _context;

    public ReportService(
        IOrderRepository orderRepository,
        IPaymentRepository paymentRepository,
        ITableRepository tableRepository,
        RestaurantDbContext context)
    {
        _orderRepository = orderRepository;
        _paymentRepository = paymentRepository;
        _tableRepository = tableRepository;
        _context = context;
    }

    public async Task<DailySalesReportDto> GetDailySalesReportAsync(DateTime date)
    {
        var payments = (await _paymentRepository.GetAllAsync())
            .Where(x => x.IsActive &&
                        x.PaymentStatus == PaymentStatus.Paid &&
                        x.PaymentDate.Date == date.Date)
            .ToList();

        var actions = await GetBillActionsAsync(date, date);
        return new DailySalesReportDto
        {
            Date = date,
            TotalBills = payments.Count,
            TotalSales = payments.Sum(x => x.BillAmount),
            TotalDiscount = payments.Sum(x => x.DiscountAmount),
            TotalTax = payments.Sum(x => x.TaxAmount),
            NetSales = payments.Sum(x => x.GrandTotal),

            CashCollection = GetCollection(payments, PaymentMode.Cash),
            CardCollection = GetCollection(payments, PaymentMode.Card),
            UpiCollection = GetCollection(payments, PaymentMode.Upi),
            CancelledBills = actions.Count(x => x.ActionType == BillActionType.Cancel),
            VoidedBills = actions.Count(x => x.ActionType == BillActionType.Void),
            VoidedAmount = actions
                .Where(x => x.ActionType == BillActionType.Void)
                .Sum(x => x.FinancialAmount)
        };
    }

    public async Task<DailySalesReportDto> GetDateRangeSalesReportAsync(
        DateTime fromDate,
        DateTime toDate)
    {
        var payments = (await _paymentRepository.GetAllAsync())
            .Where(x => x.IsActive &&
                        x.PaymentStatus == PaymentStatus.Paid &&
                        x.PaymentDate.Date >= fromDate.Date &&
                        x.PaymentDate.Date <= toDate.Date)
            .ToList();

        var actions = await GetBillActionsAsync(fromDate, toDate);
        return new DailySalesReportDto
        {
            Date = fromDate,
            TotalBills = payments.Count,
            TotalSales = payments.Sum(x => x.BillAmount),
            TotalDiscount = payments.Sum(x => x.DiscountAmount),
            TotalTax = payments.Sum(x => x.TaxAmount),
            NetSales = payments.Sum(x => x.GrandTotal),

            CashCollection = GetCollection(payments, PaymentMode.Cash),
            CardCollection = GetCollection(payments, PaymentMode.Card),
            UpiCollection = GetCollection(payments, PaymentMode.Upi),
            CancelledBills = actions.Count(x => x.ActionType == BillActionType.Cancel),
            VoidedBills = actions.Count(x => x.ActionType == BillActionType.Void),
            VoidedAmount = actions
                .Where(x => x.ActionType == BillActionType.Void)
                .Sum(x => x.FinancialAmount)
        };
    }

    public async Task<List<ItemSalesReportDto>> GetItemSalesReportAsync(
        DateTime fromDate,
        DateTime toDate)
    {
        var payments = (await _paymentRepository.GetAllAsync())
            .Where(p => p.IsActive &&
                        p.PaymentStatus == PaymentStatus.Paid &&
                        p.PaymentDate.Date >= fromDate.Date &&
                        p.PaymentDate.Date <= toDate.Date)
            .ToList();

        var paidOrderIds = payments
            .Select(p => p.OrderId)
            .ToHashSet();

        var orders = (await _orderRepository.GetAllAsync())
            .Where(o => paidOrderIds.Contains(o.Id))
            .ToList();

        return orders
            .SelectMany(o => o.Items)
            .GroupBy(i => new
            {
                i.MenuItemId,
                ItemName = i.MenuItem?.Name ?? "Unknown"
            })
            .Select(g => new ItemSalesReportDto
            {
                MenuItemId = g.Key.MenuItemId,
                ItemName = g.Key.ItemName,
                QuantitySold = g.Sum(x => x.Quantity - x.CancelledQuantity),
                TotalSales = g.Sum(x =>
                    x.Price * (x.Quantity - x.CancelledQuantity))
            })
            .OrderByDescending(x => x.QuantitySold)
            .ToList();
    }

    public async Task<List<CategorySalesReportDto>> GetCategorySalesReportAsync(
        DateTime fromDate,
        DateTime toDate)
    {
        var payments = (await _paymentRepository.GetAllAsync())
            .Where(p => p.IsActive &&
                        p.PaymentStatus == PaymentStatus.Paid &&
                        p.PaymentDate.Date >= fromDate.Date &&
                        p.PaymentDate.Date <= toDate.Date)
            .ToList();

        var paidOrderIds = payments
            .Select(p => p.OrderId)
            .ToHashSet();

        var orders = (await _orderRepository.GetAllAsync())
            .Where(o => paidOrderIds.Contains(o.Id))
            .ToList();

        return orders
            .SelectMany(o => o.Items)
            .GroupBy(i => new
            {
                CategoryId = i.MenuItem?.CategoryId ?? 0,
                CategoryName = i.MenuItem?.Category?.Name ?? "Unknown"
            })
            .Select(g => new CategorySalesReportDto
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.CategoryName,
                QuantitySold = g.Sum(x => x.Quantity - x.CancelledQuantity),
                TotalSales = g.Sum(x =>
                    x.Price * (x.Quantity - x.CancelledQuantity))
            })
            .OrderByDescending(x => x.TotalSales)
            .ToList();
    }

    public async Task<List<CaptainSalesReportDto>> GetCaptainSalesReportAsync(
        DateTime fromDate,
        DateTime toDate)
    {
        var payments = (await _paymentRepository.GetAllAsync())
            .Where(p => p.IsActive &&
                        p.PaymentStatus == PaymentStatus.Paid &&
                        p.PaymentDate.Date >= fromDate.Date &&
                        p.PaymentDate.Date <= toDate.Date)
            .ToList();

        var paidOrderIds = payments
            .Select(p => p.OrderId)
            .ToHashSet();

        var orders = (await _orderRepository.GetAllAsync())
            .Where(o => paidOrderIds.Contains(o.Id))
            .ToList();

        return orders
            .GroupBy(o => new
            {
                CaptainId = o.UserId,
                CaptainName = o.User?.FullName ?? "Unknown"
            })
            .Select(g => new CaptainSalesReportDto
            {
                CaptainId = g.Key.CaptainId,
                CaptainName = g.Key.CaptainName,
                TotalBills = g.Count(),
                TotalSales = g.Sum(x => x.GrandTotal)
            })
            .OrderByDescending(x => x.TotalSales)
            .ToList();
    }

    public async Task<List<CashierSalesReportDto>> GetCashierSalesReportAsync(
        DateTime fromDate,
        DateTime toDate)
    {
        var payments = (await _paymentRepository.GetAllAsync())
            .Where(p => p.IsActive &&
                        p.PaymentStatus == PaymentStatus.Paid &&
                        p.PaymentDate.Date >= fromDate.Date &&
                        p.PaymentDate.Date <= toDate.Date)
            .ToList();

        return payments
            .GroupBy(p => new
            {
                CashierId = p.UserId,
                CashierName = p.User?.FullName ?? "Unknown"
            })
            .Select(g => new CashierSalesReportDto
            {
                CashierId = g.Key.CashierId,
                CashierName = g.Key.CashierName,
                TotalBills = g.Count(),
                TotalCollection = g.Sum(x => x.PaidAmount)
            })
            .OrderByDescending(x => x.TotalCollection)
            .ToList();
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(DateTime date)
    {
        var payments = (await _paymentRepository.GetAllAsync())
            .Where(x => x.IsActive &&
                        x.PaymentStatus == PaymentStatus.Paid &&
                        x.PaymentDate.Date == date.Date)
            .ToList();

        var tables = (await _tableRepository.GetAllAsync()).ToList();

        return new DashboardSummaryDto
        {
            TodaySales = payments.Sum(x => x.GrandTotal),
            TodayBills = payments.Count,
            TodayCollection = payments.Sum(x => x.PaidAmount),
            PendingAmount = payments.Sum(x => x.BalanceAmount),

            TotalTables = tables.Count,
            AvailableTables = tables.Count(x => x.Status == "Available"),
            OccupiedTables = tables.Count(x =>
                x.Status == "Occupied" ||
                x.Status == "BillRequested" ||
                x.Status == "PaymentPending" ||
                x.Status == "CleaningPending"),
            ReservedTables = tables.Count(x => x.Status == "Reserved")
        };
    }

    public async Task<PaymentCollectionReportDto> GetPaymentCollectionReportAsync(
    DateTime fromDate,
    DateTime toDate)
    {
        var payments = (await _paymentRepository.GetAllAsync())
            .Where(p => p.IsActive &&
                        p.PaymentStatus == PaymentStatus.Paid &&
                        p.PaymentDate.Date >= fromDate.Date &&
                        p.PaymentDate.Date <= toDate.Date)
            .ToList();

        return new PaymentCollectionReportDto
        {
            TotalCollection = payments.Sum(x => x.PaidAmount),

            CashCollection = GetCollection(payments, PaymentMode.Cash),
            CardCollection = GetCollection(payments, PaymentMode.Card),
            UpiCollection = GetCollection(payments, PaymentMode.Upi),

            MixedCollection = payments
                .Where(x => x.PaymentMethod == PaymentMode.Mixed)
                .Sum(x => x.PaidAmount)
        };
    }

    public async Task<List<TopSellingItemDto>> GetTopSellingItemsAsync(
    DateTime fromDate,
    DateTime toDate)
    {
        var payments = (await _paymentRepository.GetAllAsync())
            .Where(p => p.IsActive &&
                        p.PaymentStatus == PaymentStatus.Paid &&
                        p.PaymentDate.Date >= fromDate.Date &&
                        p.PaymentDate.Date <= toDate.Date)
            .ToList();

        var paidOrderIds = payments
            .Select(p => p.OrderId)
            .ToHashSet();

        var orders = (await _orderRepository.GetAllAsync())
            .Where(o => paidOrderIds.Contains(o.Id))
            .ToList();

        return orders
            .SelectMany(o => o.Items)
            .GroupBy(i => new
            {
                i.MenuItemId,
                ItemName = i.MenuItem?.Name ?? "Unknown"
            })
            .Select(g => new TopSellingItemDto
            {
                MenuItemId = g.Key.MenuItemId,
                ItemName = g.Key.ItemName,
                QuantitySold = g.Sum(x => x.Quantity - x.CancelledQuantity),
                TotalSales = g.Sum(x =>
                    x.Price * (x.Quantity - x.CancelledQuantity))
            })
            .OrderByDescending(x => x.QuantitySold)
            .Take(10)
            .ToList();
    }

    private async Task<List<BillActionAudit>> GetBillActionsAsync(
        DateTime fromDate,
        DateTime toDate)
    {
        var startUtc = DateTime.SpecifyKind(fromDate.Date, DateTimeKind.Local)
            .ToUniversalTime();
        var endUtc = DateTime.SpecifyKind(toDate.Date.AddDays(1), DateTimeKind.Local)
            .ToUniversalTime();

        return await _context.BillActionAudits
            .AsNoTracking()
            .Where(x => x.ActionOn >= startUtc && x.ActionOn < endUtc)
            .ToListAsync();
    }

    private static decimal GetCollection(
        IEnumerable<Sulthan.Core.Entities.Payment> payments,
        PaymentMode paymentMode)
    {
        return payments.SelectMany(payment =>
                payment.Allocations.Count > 0
                    ? payment.Allocations.Select(x => (x.PaymentMethod, x.Amount))
                    : new[] { (payment.PaymentMethod, payment.PaidAmount) })
            .Where(x => x.PaymentMethod == paymentMode)
            .Sum(x => x.Amount);
    }
}
