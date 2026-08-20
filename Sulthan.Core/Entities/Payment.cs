using Sulthan.Core.Enums;

namespace Sulthan.Core.Entities;

public class Payment : BaseEntity
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public decimal BillAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal GrandTotal { get; set; }

    public PaymentMode PaymentMethod { get; set; }

    public PaymentStatus PaymentStatus { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal BalanceAmount { get; set; }

    public string? TransactionNumber { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.Now;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<PaymentAllocation> Allocations { get; set; } = new List<PaymentAllocation>();
}
