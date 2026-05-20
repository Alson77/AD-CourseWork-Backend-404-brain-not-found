using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsBackend.Data;
using VehiclePartsBackend.Models;

namespace VehiclePartsBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public AppointmentsController(AppDbContext context) { _context = context; }

        // GET api/appointments
        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetAll()
        {
            var appointments = await _context.Appointments
                .OrderByDescending(a => a.BookedAt)
                .ToListAsync();
            return Ok(appointments);
        }

        // GET api/appointments/my — user gets their own appointments
        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMine()
        {
            var name = User.Identity?.Name;
            var appointments = await _context.Appointments
                .Where(a => a.CustomerName == name)
                .OrderByDescending(a => a.BookedAt)
                .ToListAsync();
            return Ok(appointments);
        }

        // GET api/appointments/{id}
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            var appt = await _context.Appointments.FindAsync(id);
            if (appt == null) return NotFound(new { message = $"Appointment {id} not found." });
            return Ok(appt);
        }

        // POST api/appointments — book a new appointment (any authenticated user)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] Appointment appt)
        {
            if (string.IsNullOrWhiteSpace(appt.CustomerName))
                return BadRequest(new { message = "Customer name is required." });
            if (string.IsNullOrWhiteSpace(appt.VehicleNumber))
                return BadRequest(new { message = "Vehicle number is required." });
            if (string.IsNullOrWhiteSpace(appt.ServiceType))
                return BadRequest(new { message = "Service type is required." });
            if (string.IsNullOrWhiteSpace(appt.PreferredDate))
                return BadRequest(new { message = "Preferred date is required." });

            appt.Status = "Pending";
            appt.BookedAt = DateTime.UtcNow;
            _context.Appointments.Add(appt);
            await _context.SaveChangesAsync();
            return Ok(appt);
        }

        // PUT api/appointments/{id}/status — update status (Staff/Admin action)
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] AppointmentStatusUpdate req)
        {
            var appt = await _context.Appointments.FindAsync(id);
            if (appt == null) return NotFound(new { message = $"Appointment {id} not found." });

            var valid = new[] { "Pending", "Confirmed", "Completed", "Rejected" };
            if (!valid.Contains(req.Status))
                return BadRequest(new { message = "Status must be Pending, Confirmed, Completed, or Rejected." });

            appt.Status = req.Status;
            if (!string.IsNullOrEmpty(req.StaffNotes))
                appt.StaffNotes = req.StaffNotes;

            await _context.SaveChangesAsync();
            return Ok(appt);
        }
    }

    public class AppointmentStatusUpdate
    {
        public string Status { get; set; } = string.Empty;
        public string? StaffNotes { get; set; }
    }
}

