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
    public class VendorsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VendorsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetVendors()
        {
            return Ok(await _context.Vendors.ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> AddVendor([FromBody] Vendor vendor)
        {
            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();
            return Ok(vendor);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVendor(int id, [FromBody] Vendor vendor)
        {
            var existing = await _context.Vendors.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Name = vendor.Name;
            existing.ContactPerson = vendor.ContactPerson;
            existing.Phone = vendor.Phone;
            existing.Email = vendor.Email;
            existing.Address = vendor.Address;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVendor(int id)
        {
            var existing = await _context.Vendors.FindAsync(id);
            if (existing == null) return NotFound();

            _context.Vendors.Remove(existing);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
