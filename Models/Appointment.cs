namespace VehiclePartsBackend.Models
{
    // Represents a service appointment booking
    public class Appointment
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string VehicleNumber { get; set; } = string.Empty;
        public string VehicleModel { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public string PreferredDate { get; set; } = string.Empty;
        public string PreferredTime { get; set; } = string.Empty;
        public string IssueDescription { get; set; } = string.Empty;
        public string StaffNotes { get; set; } = string.Empty;
        // Status: Pending | Confirmed | Completed | Rejected
        public string Status { get; set; } = "Pending";
        public DateTime BookedAt { get; set; } = DateTime.UtcNow;
    }
}
