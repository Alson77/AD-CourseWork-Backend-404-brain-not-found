namespace VehiclePartsBackend.Models
{
    // Represents a vehicle part in the inventory
    public class Part
    {
        public int Id { get; set; }
        public string PartName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
