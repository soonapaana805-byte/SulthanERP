namespace Sulthan.Core.DTOs.Dashboard;

public class DashboardResponseDto
{
    public decimal TodaySales { get; set; }

    public int TodayBills { get; set; }

    public decimal TodayCollection { get; set; }

    public decimal PendingCollection { get; set; }

    public int TotalTables { get; set; }

    public int AvailableTables { get; set; }

    public int OccupiedTables { get; set; }

    public int ReservedTables { get; set; }

    public int TotalMenuItems { get; set; }

    public int AvailableMenuItems { get; set; }

    public int TotalCategories { get; set; }

    public List<TopSellingItemDto> TopSellingItems { get; set; } = new();
}

public class TopSellingItemDto
{
    public string ItemName { get; set; } = string.Empty;

    public int QuantitySold { get; set; }
}