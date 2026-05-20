using System;

namespace VehiclePartsBackend.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int PartId { get; set; }
        public string PartName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public Order Order { get; set; }
    }
}
