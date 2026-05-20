using System;

namespace VehiclePartsBackend.Models
{
    public class CreditAccount
    {
        public int Id { get; set; }
        public int AppUserId { get; set; }
        public decimal CreditLimit { get; set; }
        public decimal UsedCredit { get; set; }
        public decimal DueAmount { get; set; }

        public AppUser AppUser { get; set; }
    }
}
