using Sulthan.Core.DTOs.Reports;

namespace Sulthan.Core.Interfaces;

public interface IReportService
{
    Task<DailySalesReportDto> GetDailySalesReportAsync(DateTime date);

    Task<DailySalesReportDto> GetDateRangeSalesReportAsync(
        DateTime fromDate,
        DateTime toDate);

    Task<List<ItemSalesReportDto>> GetItemSalesReportAsync(
        DateTime fromDate,
        DateTime toDate);

    Task<List<CategorySalesReportDto>> GetCategorySalesReportAsync(
        DateTime fromDate,
        DateTime toDate);

    Task<List<CaptainSalesReportDto>> GetCaptainSalesReportAsync(
        DateTime fromDate,
        DateTime toDate);

    Task<List<CashierSalesReportDto>> GetCashierSalesReportAsync(
        DateTime fromDate,
        DateTime toDate);

    Task<DashboardSummaryDto> GetDashboardSummaryAsync(DateTime date);

    Task<PaymentCollectionReportDto> GetPaymentCollectionReportAsync(
    DateTime fromDate,
    DateTime toDate);

    Task<List<TopSellingItemDto>> GetTopSellingItemsAsync(
    DateTime fromDate,
    DateTime toDate);
}