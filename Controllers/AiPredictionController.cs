using Microsoft.AspNetCore.Mvc;
using VehiclePartsBackend.Data;
using VehiclePartsBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace VehiclePartsBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AiPredictionController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AiPredictionController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{customerId}")]
        public async Task<IActionResult> GetPredictions(int customerId)
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null) return NotFound(new { message = "Customer not found." });

            var predictions = new List<string>();

            // Rule-based AI logic
            if (customer.Mileage > 80000)
            {
                predictions.Add("High mileage detected (>80,000). AI recommends immediate Clutch and Battery inspection.");
            }
            else if (customer.Mileage > 50000)
            {
                predictions.Add("Moderate mileage detected (>50,000). AI recommends Brake Pad and Fluid inspection.");
            }

            var lastService = await _context.Appointments
                .Where(a => a.CustomerName == customer.FullName && a.Status == "Completed")
                .OrderByDescending(a => a.BookedAt)
                .FirstOrDefaultAsync();

            if (lastService == null || lastService.BookedAt < DateTime.UtcNow.AddMonths(-6))
            {
                predictions.Add("No service recorded in the last 6 months. AI recommends scheduling a General Servicing.");
            }

            if (predictions.Count == 0)
            {
                predictions.Add("Vehicle condition appears optimal based on current data.");
            }

            return Ok(new { 
                Customer = customer.FullName,
                Vehicle = $"{customer.VehicleBrand} {customer.VehicleModel}",
                Mileage = customer.Mileage,
                Predictions = predictions 
            });
        }
    }
}
