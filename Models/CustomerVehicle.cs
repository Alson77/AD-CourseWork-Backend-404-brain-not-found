namespace VehiclePartsBackend.Models
{
    public class CustomerVehicle
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string VehicleNumber { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string Mileage { get; set; } = string.Empty;
    }
}
