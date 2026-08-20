using Sulthan.Core.Common;

namespace Sulthan.Core.Entities;

public class DiningTable : BaseEntity
{
    public string TableNumber { get; set; } = string.Empty;

    public string TableType { get; set; } = string.Empty; // AC / NonAC

    public int Capacity { get; set; }

    public string Status { get; set; } = DiningTableStatus.Available;

    public int DisplayOrder { get; set; }
}
