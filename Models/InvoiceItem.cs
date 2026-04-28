namespace VehiclePartsBackend.Models
{
    // Represents one line item on an invoice
    public class InvoiceItem
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public string PartName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        // Computed — not stored in DB, included in API response
        public decimal LineTotal => Math.Round(Quantity * UnitPrice, 2);
    }
}
