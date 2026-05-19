using System;
using System.Collections.Generic;

namespace VehiclePartsBackend.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int AppUserId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal SubtotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } // e.g. "Completed", "Pending"
        public string PaymentStatus { get; set; }

        public ICollection<OrderItem> Items { get; set; }
    }
}
