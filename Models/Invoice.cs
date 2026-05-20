namespace VehiclePartsBackend.Models
{
    // Represents a sales invoice
    public class Invoice
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
        public decimal Discount { get; set; } = 0;
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
        
        public string PaymentStatus { get; set; } = "Paid";
        public DateTime? DueDate { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal BalanceAmount { get; set; }

        public List<InvoiceItem> Items { get; set; } = new();
    }
}
