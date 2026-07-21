namespace Sulthan.Core.DTOs.Tables;

public class CreateDiningTableDto
{
    public string TableNumber { get; set; } = string.Empty;

    public string TableType { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public int DisplayOrder { get; set; }
}