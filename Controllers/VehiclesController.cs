using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsBackend.Data;
using VehiclePartsBackend.Models;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace VehiclePartsBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class VehiclesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VehiclesController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        [HttpGet("my")]
        public async Task<IActionResult> GetMyVehicles()
        {
            var userId = GetUserId();
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AppUserId == userId);
            
            if (customer == null)
            {
                // Return an empty list if user is staff or customer with no profile yet.
                // Wait, if it's staff, they might not have a Customer profile. Let's get vehicles by CustomerId if it exists, otherwise return empty.
                return Ok(new object[] {});
            }

            var vehicles = await _context.CustomerVehicles
                .Where(v => v.CustomerId == customer.Id)
                .ToListAsync();

            return Ok(vehicles);
        }

        [HttpPost]
        public async Task<IActionResult> AddVehicle([FromBody] CustomerVehicle vehicle)
        {
            var userId = GetUserId();
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AppUserId == userId);

            if (customer == null)
            {
                // Create a basic customer profile for staff if they add a vehicle
                customer = new Customer
                {
                    AppUserId = userId,
                    FullName = User.FindFirstValue(ClaimTypes.Name) ?? "Staff User",
                    Email = User.FindFirstValue(ClaimTypes.Email) ?? "",
                    Phone = "0000000000",
                    RegisteredDate = System.DateTime.UtcNow
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            if (await _context.CustomerVehicles.AnyAsync(v => v.VehicleNumber.ToLower() == vehicle.VehicleNumber.ToLower()))
            {
                return BadRequest(new { message = "This vehicle number is already registered in the system." });
            }

            vehicle.CustomerId = customer.Id;
            _context.CustomerVehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            return Ok(vehicle);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVehicle(int id, [FromBody] CustomerVehicle update)
        {
            var vehicle = await _context.CustomerVehicles.FindAsync(id);
            if (vehicle == null) return NotFound();

            var userId = GetUserId();
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AppUserId == userId);
            
            if (customer == null || vehicle.CustomerId != customer.Id)
                return Forbid();

            if (await _context.CustomerVehicles.AnyAsync(v => v.VehicleNumber.ToLower() == update.VehicleNumber.ToLower() && v.Id != id))
            {
                return BadRequest(new { message = "This vehicle number is already registered in the system." });
            }

            vehicle.Brand = update.Brand;
            vehicle.Model = update.Model;
            vehicle.Year = update.Year;
            vehicle.VehicleNumber = update.VehicleNumber;
            vehicle.Mileage = update.Mileage;

            await _context.SaveChangesAsync();
            return Ok(vehicle);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVehicle(int id)
        {
            var vehicle = await _context.CustomerVehicles.FindAsync(id);
            if (vehicle == null) return NotFound();

            var userId = GetUserId();
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AppUserId == userId);
            
            if (customer == null || vehicle.CustomerId != customer.Id)
                return Forbid();

            _context.CustomerVehicles.Remove(vehicle);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
