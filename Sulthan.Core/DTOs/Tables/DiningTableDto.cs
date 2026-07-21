namespace Sulthan.Core.DTOs.Tables;

public class DiningTableDto
{
    public int Id { get; set; }

    public string TableCode { get; set; } = string.Empty;

    public bool IsAc { get; set; }

    public int Capacity { get; set; }

    public bool IsOccupied { get; set; }

    public bool IsActive { get; set; }
}