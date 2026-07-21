using Sulthan.Core.Enums;

namespace Sulthan.Core.Entities;

public class Payment : BaseEntity
{
    public int OrderId { get; set; }

    public Order Order { get; set; } = null!;

    public decimal Amount { get; set; }

    public PaymentMode PaymentMode { get; set; } = PaymentMode.Cash;

    public DateTime PaidOn { get; set; } = DateTime.UtcNow;
}