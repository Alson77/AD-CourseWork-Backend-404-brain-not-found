namespace VehiclePartsBackend.Models
{
    // Represents a registered customer profile
    public class Customer
    {
        public int Id { get; set; }
        public int? AppUserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string? Password { get; set; }

        // Vehicle details
        public string VehicleNumber { get; set; } = string.Empty;
        public string VehicleBrand { get; set; } = string.Empty;
        public string VehicleModel { get; set; } = string.Empty;
        public string VehicleYear { get; set; } = string.Empty;
        public int Mileage { get; set; }
        public decimal PendingCredit { get; set; }
        public DateTime RegisteredDate { get; set; } = DateTime.UtcNow;
    }
}
