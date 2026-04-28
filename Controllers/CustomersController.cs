using Microsoft.AspNetCore.Mvc;
using VehiclePartsBackend.Models;

namespace VehiclePartsBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        // ── In-memory store (runs without PostgreSQL) ──────────────────────
        private static int _nextId = 2;
        private static readonly List<Customer> _customers = new()
        {
            new Customer
            {
                Id = 1,
                FullName = "John Doe",
                Email = "johndoe@example.com",
                Phone = "9800000001",
                Address = "Kathmandu",
                VehicleNumber = "BA 1 CHA 1234",
                VehicleBrand = "Toyota",
                VehicleModel = "Corolla",
                VehicleYear = "2018",
                RegisteredDate = DateTime.UtcNow.AddDays(-5)
            }
        };

        // GET api/customers
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_customers);
        }

        // POST api/customers
        [HttpPost]
        public IActionResult Create([FromBody] Customer customer)
        {
            if (string.IsNullOrWhiteSpace(customer.FullName))
                return BadRequest(new { message = "Full name is required." });
            if (string.IsNullOrWhiteSpace(customer.Phone))
                return BadRequest(new { message = "Phone number is required." });

            customer.Id = _nextId++;
            customer.RegisteredDate = DateTime.UtcNow;
            _customers.Add(customer);

            return Ok(customer);
        }
    }
}
