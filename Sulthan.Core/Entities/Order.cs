using Sulthan.Core.Enums;

namespace Sulthan.Core.Entities
{
    public class Order : BaseEntity
    {
        public string BillNumber { get; set; } = string.Empty;

        public OrderType OrderType { get; set; }

        public OrderStatus BillStatus { get; set; }

        public int DiningTableId { get; set; }
        public DiningTable? DiningTable { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public decimal SubTotal { get; set; }

        public decimal Discount { get; set; }

        public decimal Tax { get; set; }

        public decimal GrandTotal { get; set; }

        public string? Remarks { get; set; }

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}