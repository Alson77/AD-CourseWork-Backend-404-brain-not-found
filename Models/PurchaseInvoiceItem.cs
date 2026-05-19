namespace VehiclePartsBackend.Models
{
    public class PurchaseInvoiceItem
    {
        public int Id { get; set; }
        public int PurchaseInvoiceId { get; set; }
        public int PartId { get; set; }
        public Part? Part { get; set; }
        public int Quantity { get; set; }
        public decimal CostPrice { get; set; }
        public decimal LineTotal { get; set; } // We will map this to the DB
    }
}
