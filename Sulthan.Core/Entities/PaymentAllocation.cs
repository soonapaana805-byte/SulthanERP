using Sulthan.Core.Enums;

namespace Sulthan.Core.Entities;

public class PaymentAllocation : BaseEntity
{
    public int PaymentId { get; set; }

    public Payment Payment { get; set; } = null!;

    public PaymentMode PaymentMethod { get; set; }

    // Amount applied to the bill. It never includes customer change.
    public decimal Amount { get; set; }

    public decimal TenderedAmount { get; set; }

    public decimal ChangeAmount { get; set; }

    public string? TransactionNumber { get; set; }
}
