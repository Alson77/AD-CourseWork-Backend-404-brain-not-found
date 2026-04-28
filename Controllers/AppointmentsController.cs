using Microsoft.AspNetCore.Mvc;
using VehiclePartsBackend.Models;

namespace VehiclePartsBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentsController : ControllerBase
    {
        // ── In-memory store (runs without PostgreSQL) ──────────────────────
        private static int _nextId = 3;
        private static readonly List<Appointment> _appointments = new()
        {
            new Appointment { Id = 1, CustomerName = "Raju Sharma",   VehicleNumber = "BA 1 CHA 1234", ServiceType = "Oil Change",       PreferredDate = "2026-05-01", PreferredTime = "10:00", IssueDescription = "Engine oil is overdue for a change.", Status = "Confirmed",  BookedAt = DateTime.UtcNow.AddDays(-2) },
            new Appointment { Id = 2, CustomerName = "Sita Gurung",   VehicleNumber = "GA 2 KHA 5678", ServiceType = "Brake Repair",     PreferredDate = "2026-05-03", PreferredTime = "14:00", IssueDescription = "Brake pads worn out, squeaking noise.",   Status = "Pending",    BookedAt = DateTime.UtcNow.AddDays(-1) },
        };

        // GET api/appointments
        [HttpGet]
        public IActionResult GetAll() => Ok(_appointments);

        // GET api/appointments/{id}
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var appt = _appointments.FirstOrDefault(a => a.Id == id);
            if (appt == null) return NotFound(new { message = $"Appointment {id} not found." });
            return Ok(appt);
        }

        // POST api/appointments — book a new appointment
        [HttpPost]
        public IActionResult Create([FromBody] Appointment appt)
        {
            if (string.IsNullOrWhiteSpace(appt.CustomerName))
                return BadRequest(new { message = "Customer name is required." });
            if (string.IsNullOrWhiteSpace(appt.VehicleNumber))
                return BadRequest(new { message = "Vehicle number is required." });
            if (string.IsNullOrWhiteSpace(appt.ServiceType))
                return BadRequest(new { message = "Service type is required." });
            if (string.IsNullOrWhiteSpace(appt.PreferredDate))
                return BadRequest(new { message = "Preferred date is required." });

            appt.Id       = _nextId++;
            appt.Status   = "Pending";
            appt.BookedAt = DateTime.UtcNow;
            _appointments.Add(appt);

            return Ok(appt);
        }

        // PUT api/appointments/{id}/status — update status (Admin action)
        [HttpPut("{id}/status")]
        public IActionResult UpdateStatus(int id, [FromBody] StatusUpdateRequest req)
        {
            var appt = _appointments.FirstOrDefault(a => a.Id == id);
            if (appt == null) return NotFound(new { message = $"Appointment {id} not found." });

            var valid = new[] { "Pending", "Confirmed", "Completed" };
            if (!valid.Contains(req.Status))
                return BadRequest(new { message = "Status must be Pending, Confirmed, or Completed." });

            appt.Status = req.Status;
            return Ok(appt);
        }
    }

    // Request body for status update
    public class StatusUpdateRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}
