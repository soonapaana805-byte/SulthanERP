using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sulthan.Core.Interfaces;

namespace SulthanERP.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("daily")]
    public async Task<IActionResult> DailyReport([FromQuery] DateTime date)
    {
        var report = await _reportService.GetDailySalesReportAsync(date);
        return Ok(report);
    }

    [HttpGet("date-range")]
    public async Task<IActionResult> DateRangeReport(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate)
    {
        var report = await _reportService
            .GetDateRangeSalesReportAsync(fromDate, toDate);

        return Ok(report);
    }

    [HttpGet("item-sales")]
    public async Task<IActionResult> ItemSalesReport(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate)
    {
        var report = await _reportService
            .GetItemSalesReportAsync(fromDate, toDate);

        return Ok(report);
    }

    [HttpGet("category-sales")]
    public async Task<IActionResult> CategorySalesReport(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate)
    {
        var report = await _reportService
            .GetCategorySalesReportAsync(fromDate, toDate);

        return Ok(report);
    }

    [HttpGet("captain-sales")]
    public async Task<IActionResult> CaptainSalesReport(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate)
    {
        var report = await _reportService
            .GetCaptainSalesReportAsync(fromDate, toDate);

        return Ok(report);
    }

    [HttpGet("cashier-sales")]
    public async Task<IActionResult> CashierSalesReport(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate)
    {
        var report = await _reportService
            .GetCashierSalesReportAsync(fromDate, toDate);

        return Ok(report);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> DashboardSummary([FromQuery] DateTime date)
    {
        var report = await _reportService
            .GetDashboardSummaryAsync(date);

        return Ok(report);
    }

    [HttpGet("payment-collection")]
    public async Task<IActionResult> PaymentCollectionReport(
    [FromQuery] DateTime fromDate,
    [FromQuery] DateTime toDate)
    {
        var report = await _reportService
            .GetPaymentCollectionReportAsync(fromDate, toDate);

        return Ok(report);
    }

    [HttpGet("top-selling-items")]
    public async Task<IActionResult> TopSellingItemsReport(
    [FromQuery] DateTime fromDate,
    [FromQuery] DateTime toDate)
    {
        var report = await _reportService
            .GetTopSellingItemsAsync(fromDate, toDate);

        return Ok(report);
    }
}