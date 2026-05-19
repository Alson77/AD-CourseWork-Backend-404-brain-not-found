namespace VehiclePartsBackend.Models
{
    public class PurchaseInvoice
    {
        public int Id { get; set; }
        public int VendorId { get; set; }
        public Vendor? Vendor { get; set; }
        public DateTime PurchaseDate { get; set; }
        public decimal TotalAmount { get; set; }
        
        public List<PurchaseInvoiceItem> Items { get; set; } = new();
    }
}
