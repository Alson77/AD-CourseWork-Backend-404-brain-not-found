using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsBackend.Data;
using VehiclePartsBackend.Models;

namespace VehiclePartsBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class StaffController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StaffController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetStaff()
        {
            var staff = await _context.Users.ToListAsync();
            // Don't send passwords back
            var result = staff.Select(s => new { s.Id, s.Name, s.Email, s.Role, s.IsActive });
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateStaff([FromBody] AppUser user)
        {
            if (string.IsNullOrWhiteSpace(user.Role)) user.Role = "Staff";
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(new { user.Id, user.Name, user.Email, user.Role, user.IsActive });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStaff(int id, [FromBody] AppUser user)
        {
            var existing = await _context.Users.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Name = user.Name;
            existing.Email = user.Email;
            if (!string.IsNullOrEmpty(user.Password))
                existing.Password = user.Password;
            existing.Role = user.Role;
            existing.IsActive = user.IsActive;

            await _context.SaveChangesAsync();
            return Ok(new { existing.Id, existing.Name, existing.Email, existing.Role, existing.IsActive });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStaff(int id)
        {
            var existing = await _context.Users.FindAsync(id);
            if (existing == null) return NotFound();
            
            // Soft delete
            existing.IsActive = false;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
