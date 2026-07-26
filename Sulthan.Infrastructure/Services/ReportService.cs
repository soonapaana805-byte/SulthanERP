using Sulthan.Core.DTOs.Reports;
using Sulthan.Core.Enums;
using Sulthan.Core.Interfaces;

namespace Sulthan.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ITableRepository _tableRepository;

    public ReportService(
        IOrderRepository orderRepository,
        IPaymentRepository paymentRepository,
        ITableRepository tableRepository)
    {
        _orderRepository = orderRepository;
        _paymentRepository = paymentRepository;
        _tableRepository = tableRepository;
    }

    public async Task<DailySalesReportDto> GetDailySalesReportAsync(DateTime date)
    {
        var payments = (await _paymentRepository.GetAllAsync())
            .Where(x => x.PaymentDate.Date == date.Date)
            .ToList();

        return new DailySalesReportDto
        {
            Date = date,
            TotalBills = payments.Count,
            TotalSales = payments.Sum(x => x.BillAmount),
            TotalDiscount = payments.Sum(x => x.DiscountAmount),
            TotalTax = payments.Sum(x => x.TaxAmount),
            NetSales = payments.Sum(x => x.GrandTotal),

            CashCollection = payments
                .Where(x => x.PaymentMethod == PaymentMode.Cash)
                .Sum(x => x.PaidAmount),

            CardCollection = payments
                .Where(x => x.PaymentMethod == PaymentMode.Card)
                .Sum(x => x.PaidAmount),

            UpiCollection = payments
                .Where(x => x.PaymentMethod == PaymentMode.Upi)
                .Sum(x => x.PaidAmount)
        };
    }

    public async Task<DailySalesReportDto> GetDateRangeSalesReportAsync(
        DateTime fromDate,
        DateTime toDate)
    {
        var payments = (await _paymentRepository.GetAllAsync())
            .Where(x => x.PaymentDate.Date >= fromDate.Date &&
                        x.PaymentDate.Date <= toDate.Date)
            .ToList();

        return new DailySalesReportDto
        {
            Date = fromDate,
            TotalBills = payments.Count,
            TotalSales = payments.Sum(x => x.BillAmount),
            TotalDiscount = payments.Sum(x => x.DiscountAmount),
            TotalTax = payments.Sum(x => x.TaxAmount),
            NetSales = payments.Sum(x => x.GrandTotal),

            CashCollection = payments
                .Where(x => x.PaymentMethod == PaymentMode.Cash)
                .Sum(x => x.PaidAmount),

            CardCollection = payments
                .Where(x => x.PaymentMethod == PaymentMode.Card)
                .Sum(x => x.PaidAmount),

            UpiCollection = payments
                .Where(x => x.PaymentMethod == PaymentMode.Upi)
                .Sum(x => x.PaidAmount)
        };
    }

    public async Task<List<ItemSalesReportDto>> GetItemSalesReportAsync(
        DateTime fromDate,
        DateTime toDate)
    {
        var payments = (await _paymentRepository.GetAllAsync())
            .Where(p => p.PaymentDate.Date >= fromDate.Date &&
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
                QuantitySold = g.Sum(x => x.Quantity),
                TotalSales = g.Sum(x => x.Price * x.Quantity)
            })
            .OrderByDescending(x => x.QuantitySold)
            .ToList();
    }

    public async Task<List<CategorySalesReportDto>> GetCategorySalesReportAsync(
        DateTime fromDate,
        DateTime toDate)
    {
        var payments = (await _paymentRepository.GetAllAsync())
            .Where(p => p.PaymentDate.Date >= fromDate.Date &&
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
                QuantitySold = g.Sum(x => x.Quantity),
                TotalSales = g.Sum(x => x.Price * x.Quantity)
            })
            .OrderByDescending(x => x.TotalSales)
            .ToList();
    }

    public async Task<List<CaptainSalesReportDto>> GetCaptainSalesReportAsync(
        DateTime fromDate,
        DateTime toDate)
    {
        var payments = (await _paymentRepository.GetAllAsync())
            .Where(p => p.PaymentDate.Date >= fromDate.Date &&
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
            .Where(p => p.PaymentDate.Date >= fromDate.Date &&
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
            .Where(x => x.PaymentDate.Date == date.Date)
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
            OccupiedTables = tables.Count(x => x.Status == "Occupied"),
            ReservedTables = tables.Count(x => x.Status == "Reserved")
        };
    }

    public async Task<PaymentCollectionReportDto> GetPaymentCollectionReportAsync(
    DateTime fromDate,
    DateTime toDate)
    {
        var payments = (await _paymentRepository.GetAllAsync())
            .Where(p => p.PaymentDate.Date >= fromDate.Date &&
                        p.PaymentDate.Date <= toDate.Date)
            .ToList();

        return new PaymentCollectionReportDto
        {
            TotalCollection = payments.Sum(x => x.PaidAmount),

            CashCollection = payments
                .Where(x => x.PaymentMethod == PaymentMode.Cash)
                .Sum(x => x.PaidAmount),

            CardCollection = payments
                .Where(x => x.PaymentMethod == PaymentMode.Card)
                .Sum(x => x.PaidAmount),

            UpiCollection = payments
                .Where(x => x.PaymentMethod == PaymentMode.Upi)
                .Sum(x => x.PaidAmount),

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
            .Where(p => p.PaymentDate.Date >= fromDate.Date &&
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
                QuantitySold = g.Sum(x => x.Quantity),
                TotalSales = g.Sum(x => x.Price * x.Quantity)
            })
            .OrderByDescending(x => x.QuantitySold)
            .Take(10)
            .ToList();
    }
}