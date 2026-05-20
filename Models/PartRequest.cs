namespace VehiclePartsBackend.Models
{
    public class PartRequest
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public AppUser? User { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string VehicleModel { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string AdminNote { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
