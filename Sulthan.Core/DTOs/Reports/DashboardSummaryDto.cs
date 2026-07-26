namespace Sulthan.Core.DTOs.Reports;

public class DashboardSummaryDto
{
    public decimal TodaySales { get; set; }

    public int TodayBills { get; set; }

    public decimal TodayCollection { get; set; }

    public decimal PendingAmount { get; set; }

    public int TotalTables { get; set; }

    public int AvailableTables { get; set; }

    public int OccupiedTables { get; set; }

    public int ReservedTables { get; set; }
}