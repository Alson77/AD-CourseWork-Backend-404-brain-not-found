using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsBackend.Data;
using VehiclePartsBackend.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace VehiclePartsBackend.Controllers
{
    [ApiController]
    [Route("api/partrequests")]
    [Authorize]
    public class PartRequestsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public PartRequestsController(AppDbContext context) { _context = context; }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var requests = await _context.PartRequests
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            return Ok(requests);
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyRequests()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var requests = await _context.PartRequests
                .Where(pr => pr.UserId == userId)
                .OrderByDescending(pr => pr.CreatedAt)
                .ToListAsync();

            return Ok(requests);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var req = await _context.PartRequests.FindAsync(id);
            if (req == null) return NotFound();

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = User.FindFirstValue(ClaimTypes.Role);
            int.TryParse(userIdStr, out int userId);

            if (role != "Admin" && req.UserId != userId)
                return Forbid();

            return Ok(req);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PartRequest request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = User.FindFirstValue(ClaimTypes.Role);
            var name = User.FindFirstValue(ClaimTypes.Name);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            request.UserId = userId;
            request.UserName = name ?? "Unknown";
            request.UserRole = role ?? "Unknown";
            request.CreatedAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;
            request.Status = "Pending";

            _context.PartRequests.Add(request);
            await _context.SaveChangesAsync();
            return Ok(request);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PartRequest update)
        {
            var req = await _context.PartRequests.FindAsync(id);
            if (req == null) return NotFound();

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdStr, out int userId);

            if (req.UserId != userId) return Forbid();
            if (req.Status != "Pending") return BadRequest("Can only update pending requests.");

            req.PartName = update.PartName;
            req.Brand = update.Brand;
            req.VehicleModel = update.VehicleModel;
            req.Quantity = update.Quantity;
            req.Description = update.Description;
            req.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(req);
        }

        public class StatusUpdateDto
        {
            public string Status { get; set; } = string.Empty;
            public string AdminNote { get; set; } = string.Empty;
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] StatusUpdateDto dto)
        {
            var req = await _context.PartRequests.FindAsync(id);
            if (req == null) return NotFound();

            if (!string.IsNullOrEmpty(dto.Status)) req.Status = dto.Status;
            if (dto.AdminNote != null) req.AdminNote = dto.AdminNote;
            req.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(req);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var req = await _context.PartRequests.FindAsync(id);
            if (req == null) return NotFound();

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = User.FindFirstValue(ClaimTypes.Role);
            int.TryParse(userIdStr, out int userId);

            if (role != "Admin" && req.UserId != userId) return Forbid();
            if (role != "Admin" && req.Status != "Pending") return BadRequest("Can only delete pending requests.");

            _context.PartRequests.Remove(req);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
