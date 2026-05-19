using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VehiclePartsBackend.Data;
using VehiclePartsBackend.Models;

namespace VehiclePartsBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Customer")]
    public class CustomerProfileController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CustomerProfileController(AppDbContext context)
        {
            _context = context;
        }

        private int GetAppUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            int userId = GetAppUserId();
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AppUserId == userId);
            
            if (customer == null) return NotFound("Profile not found.");

            var vehicles = await _context.CustomerVehicles
                .Where(v => v.CustomerId == customer.Id)
                .ToListAsync();

            return Ok(new { Profile = customer, Vehicles = vehicles });
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] Customer update)
        {
            int userId = GetAppUserId();
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AppUserId == userId);
            
            if (customer == null) return NotFound("Profile not found.");

            customer.FullName = update.FullName;
            customer.Email = update.Email;
            customer.Phone = update.Phone;
            customer.Address = update.Address;

            // Also update AppUser name/email if changed
            var appUser = await _context.Users.FindAsync(userId);
            if (appUser != null)
            {
                appUser.Name = update.FullName;
                appUser.Email = update.Email;
            }

            await _context.SaveChangesAsync();
            return Ok(customer);
        }

        [HttpPost("me/vehicles")]
        public async Task<IActionResult> AddVehicle([FromBody] CustomerVehicle vehicle)
        {
            int userId = GetAppUserId();
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AppUserId == userId);
            if (customer == null) return NotFound();

            vehicle.CustomerId = customer.Id;
            _context.CustomerVehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            return Ok(vehicle);
        }

        [HttpDelete("me/vehicles/{vid}")]
        public async Task<IActionResult> DeleteVehicle(int vid)
        {
            int userId = GetAppUserId();
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AppUserId == userId);
            if (customer == null) return NotFound();

            var vehicle = await _context.CustomerVehicles.FirstOrDefaultAsync(v => v.Id == vid && v.CustomerId == customer.Id);
            if (vehicle == null) return NotFound();

            _context.CustomerVehicles.Remove(vehicle);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
