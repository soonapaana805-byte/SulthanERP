namespace Sulthan.Core.Entities;

public class BillCounter : BaseEntity
{
    public DateOnly BusinessDate { get; set; }

    public int LastBillNumber { get; set; }
}