namespace Sulthan.Core.DTOs.Tables;

public class UpdateDiningTableDto
{
    public string TableNumber { get; set; } = string.Empty;

    public string TableType { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public string Status { get; set; } = "Available";

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }
}