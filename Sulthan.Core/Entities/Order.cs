using Sulthan.Core.Enums;

namespace Sulthan.Core.Entities
{
    public class Order : BaseEntity
    {
        public string BillNumber { get; set; } = string.Empty;

        public OrderType OrderType { get; set; }

        public OrderStatus BillStatus { get; set; }

        public int? DiningTableId { get; set; }
        public DiningTable? DiningTable { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        // Snapshot captured at order creation so receipts remain accurate even if a customer record changes later.
        public string? CustomerName { get; set; }

        public decimal SubTotal { get; set; }

        public decimal Discount { get; set; }

        public decimal Tax { get; set; }

        public decimal GrandTotal { get; set; }

        public string? Remarks { get; set; }

        /// <summary>UTC time when the Captain requested the customer bill.</summary>
        public DateTime? BillRequestedOn { get; set; }

        /// <summary>UTC time when the Cashier completed the customer-bill print action.</summary>
        public DateTime? BillPrintedOn { get; set; }

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
