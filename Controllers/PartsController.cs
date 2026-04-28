using Microsoft.AspNetCore.Mvc;
using VehiclePartsBackend.Models;

namespace VehiclePartsBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PartsController : ControllerBase
    {
        // ── In-memory store (runs without PostgreSQL) ──────────────────────
        // To switch to PostgreSQL: inject AppDbContext and replace _parts with _db.Parts
        // ──────────────────────────────────────────────────────────────────
        private static int _nextId = 4;
        private static readonly List<Part> _parts = new()
        {
            new Part { Id = 1, PartName = "Oil Filter",  Brand = "Bosch",  Category = "Filters",    Price = 850.00m,  StockQuantity = 50, CreatedDate = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) },
            new Part { Id = 2, PartName = "Air Filter",  Brand = "Denso",  Category = "Filters",    Price = 650.00m,  StockQuantity = 35, CreatedDate = new DateTime(2026,1,2,0,0,0,DateTimeKind.Utc) },
            new Part { Id = 3, PartName = "Brake Pads",  Brand = "Brembo", Category = "Brakes",     Price = 2200.00m, StockQuantity = 20, CreatedDate = new DateTime(2026,1,3,0,0,0,DateTimeKind.Utc) },
        };

        // GET api/parts
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_parts);
        }

        // GET api/parts/{id}
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var part = _parts.FirstOrDefault(p => p.Id == id);
            if (part == null)
                return NotFound(new { message = $"Part with ID {id} was not found." });
            return Ok(part);
        }

        // POST api/parts
        [HttpPost]
        public IActionResult Create([FromBody] Part part)
        {
            if (string.IsNullOrWhiteSpace(part.PartName))
                return BadRequest(new { message = "Part name is required." });
            if (part.Price <= 0)
                return BadRequest(new { message = "Price must be greater than zero." });
            if (part.StockQuantity < 0)
                return BadRequest(new { message = "Stock quantity cannot be negative." });

            part.Id = _nextId++;
            part.CreatedDate = DateTime.UtcNow;
            _parts.Add(part);

            return CreatedAtAction(nameof(GetById), new { id = part.Id }, part);
        }

        // PUT api/parts/{id}
        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] Part updated)
        {
            var part = _parts.FirstOrDefault(p => p.Id == id);
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

            return Ok(part);
        }

        // DELETE api/parts/{id}
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var part = _parts.FirstOrDefault(p => p.Id == id);
            if (part == null)
                return NotFound(new { message = $"Part with ID {id} was not found." });

            _parts.Remove(part);
            return Ok(new { message = "Part deleted successfully." });
        }
    }
}
