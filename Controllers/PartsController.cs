using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsBackend.Data;
using VehiclePartsBackend.Models;
using Microsoft.AspNetCore.Authorization;

namespace VehiclePartsBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PartsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PartsController(AppDbContext context)
        {
            _context = context;
        }

        // GET api/parts
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var parts = await _context.Parts.ToListAsync();
            return Ok(parts);
        }

        [HttpGet("fix-typo")]
        [AllowAnonymous]
        public async Task<IActionResult> FixTypo()
        {
            var parts = await _context.Parts.Where(p => p.PartName.Contains("Brae")).ToListAsync();
            foreach (var p in parts)
            {
                p.PartName = p.PartName.Replace("Brae", "Brake");
            }
            await _context.SaveChangesAsync();
            return Ok(new { message = "Fixed typo", count = parts.Count });
        }

        // GET api/parts/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var part = await _context.Parts.FindAsync(id);
            if (part == null)
                return NotFound(new { message = $"Part with ID {id} was not found." });
            return Ok(part);
        }

        // POST api/parts
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Part part)
        {
            if (string.IsNullOrWhiteSpace(part.PartName))
                return BadRequest(new { message = "Part name is required." });
            if (part.Price <= 0)
                return BadRequest(new { message = "Price must be greater than zero." });
            if (part.StockQuantity < 0)
                return BadRequest(new { message = "Stock quantity cannot be negative." });

            part.CreatedDate = DateTime.UtcNow;
            _context.Parts.Add(part);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = part.Id }, part);
        }

        // PUT api/parts/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Part updated)
        {
            var part = await _context.Parts.FindAsync(id);
            if (part == null)
                return NotFound(new { message = $"Part with ID {id} was not found." });

            if (string.IsNullOrWhiteSpace(updated.PartName))
                return BadRequest(new { message = "Part name is required." });
            if (updated.Price <= 0)
                return BadRequest(new { message = "Price must be greater than zero." });
            if (updated.StockQuantity < 0)
                return BadRequest(new { message = "Stock quantity cannot be negative." });

            part.PartName      = updated.PartName;
            part.Brand         = updated.Brand;
            part.Category      = updated.Category;
            part.Price         = updated.Price;
            part.StockQuantity = updated.StockQuantity;

            await _context.SaveChangesAsync();
            return Ok(part);
        }

        // DELETE api/parts/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var part = await _context.Parts.FindAsync(id);
            if (part == null)
                return NotFound(new { message = $"Part with ID {id} was not found." });

            _context.Parts.Remove(part);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Part deleted successfully." });
        }
    }
}
