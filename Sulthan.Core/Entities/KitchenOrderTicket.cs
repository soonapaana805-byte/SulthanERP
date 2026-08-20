using Sulthan.Core.Common;

namespace Sulthan.Core.Entities;

public class KitchenOrderTicket : BaseEntity
{
    public string KotNumber { get; set; } = string.Empty;

    public int OrderId { get; set; }

    public Order? Order { get; set; }

    public DateTime PrintedOn { get; set; } = DateTime.Now;

    public bool IsReprint { get; set; } = false;

    public string Status { get; set; } = KitchenOrderTicketStatus.Active;

    public DateTime? CancelledOn { get; set; }

    public ICollection<KitchenOrderTicketItem> Items { get; set; } = new List<KitchenOrderTicketItem>();
}
