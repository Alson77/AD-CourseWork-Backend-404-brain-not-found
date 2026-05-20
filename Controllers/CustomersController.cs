using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsBackend.Data;
using VehiclePartsBackend.Models;

namespace VehiclePartsBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CustomersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? search)
        {
            var query = _context.Customers.AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(c => 
                    c.FullName.ToLower().Contains(lowerSearch) ||
                    c.Phone.Contains(lowerSearch) ||
                    c.VehicleNumber.ToLower().Contains(lowerSearch) ||
                    c.Id.ToString() == lowerSearch
                );
            }
            return Ok(await query.ToListAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();

            var invoices = await _context.Invoices
                .Include(i => i.Items)
                .Where(i => i.CustomerPhone == customer.Phone || i.CustomerName == customer.FullName)
                .ToListAsync();
            var appointments = await _context.Appointments
                .Where(a => a.CustomerName == customer.FullName)
                .ToListAsync();
            var reviews = await _context.Reviews
                .Where(r => r.CustomerId == id)
                .ToListAsync();
            var partRequests = customer.AppUserId.HasValue 
                ? await _context.PartRequests.Where(pr => pr.UserId == customer.AppUserId.Value).ToListAsync()
                : new List<PartRequest>();

            return Ok(new {
                Profile = customer,
                Invoices = invoices,
                Appointments = appointments,
                Reviews = reviews,
                PartRequests = partRequests
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Customer customer)
        {
            if (string.IsNullOrWhiteSpace(customer.FullName) || string.IsNullOrWhiteSpace(customer.Phone))
                return BadRequest(new { message = "Full name and Phone are required." });

            if (string.IsNullOrWhiteSpace(customer.Email))
                return BadRequest(new { message = "Email is required to create a login account." });

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == customer.Email.ToLower()))
                return BadRequest(new { message = "Email is already registered." });

            // Create AppUser for login
            var newUser = new AppUser
            {
                Name = customer.FullName,
                Email = customer.Email,
                Password = string.IsNullOrWhiteSpace(customer.Password) ? "Password123" : customer.Password,
                Role = "Customer",
                IsActive = true
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Link Customer to AppUser
            customer.AppUserId = newUser.Id;
            customer.RegisteredDate = DateTime.UtcNow;
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return Ok(customer);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Customer update)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();

            customer.FullName = update.FullName;
            customer.Email = update.Email;
            customer.Phone = update.Phone;
            customer.Address = update.Address;
            customer.VehicleNumber = update.VehicleNumber;
            customer.VehicleBrand = update.VehicleBrand;
            customer.VehicleModel = update.VehicleModel;
            customer.VehicleYear = update.VehicleYear;
            customer.Mileage = update.Mileage;
            customer.PendingCredit = update.PendingCredit;

            await _context.SaveChangesAsync();
            return Ok(customer);
        }

        [HttpGet("lookup")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> LookupByPhone([FromQuery] string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return BadRequest(new { message = "Phone is required." });

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Phone == phone.Trim());
            if (customer == null)
                return NotFound(new { message = "No customer found with this phone." });

            return Ok(new
            {
                name = customer.FullName,
                email = customer.Email,
                phone = customer.Phone
            });
        }

        [HttpGet("reports/regulars")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetRegulars()
        {
            var byPhone = await GetCustomersByPhoneAsync();
            var invoices = await _context.Invoices
                .Where(i => !string.IsNullOrEmpty(i.CustomerPhone))
                .ToListAsync();

            var regulars = invoices
                .GroupBy(i => i.CustomerPhone!.Trim())
                .Where(g => g.Count() >= 2)
                .Select(g => new
                {
                    name = ResolveCustomerName(g, byPhone),
                    phone = g.Key,
                    purchaseCount = g.Count(),
                    totalSpent = g.Sum(x => x.Total)
                })
                .OrderByDescending(x => x.totalSpent)
                .ToList();

            return Ok(regulars);
        }

        [HttpGet("reports/highspenders")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetHighSpenders()
        {
            var byPhone = await GetCustomersByPhoneAsync();
            var invoices = await _context.Invoices
                .Where(i => !string.IsNullOrEmpty(i.CustomerPhone))
                .ToListAsync();

            var spenders = invoices
                .GroupBy(i => i.CustomerPhone!.Trim())
                .Select(g => new
                {
                    name = ResolveCustomerName(g, byPhone),
                    phone = g.Key,
                    totalSpent = g.Sum(x => x.Total)
                })
                .Where(g => g.totalSpent >= 5000)
                .OrderByDescending(g => g.totalSpent)
                .ToList();

            return Ok(spenders);
        }

        [HttpGet("reports/pendingcredit")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetPendingCredit()
        {
            var byPhone = await GetCustomersByPhoneAsync();
            var unpaid = await _context.Invoices
                .Where(i => i.BalanceAmount > 0)
                .ToListAsync();

            var pending = unpaid
                .GroupBy(i => !string.IsNullOrWhiteSpace(i.CustomerPhone)
                    ? i.CustomerPhone.Trim()
                    : i.CustomerName.Trim().ToLower())
                .Select(g => new
                {
                    name = ResolveCustomerName(g, byPhone),
                    phone = g.First().CustomerPhone,
                    pendingCredit = g.Sum(x => x.BalanceAmount),
                    invoiceCount = g.Count()
                })
                .OrderByDescending(x => x.pendingCredit)
                .ToList();

            return Ok(pending);
        }

        private async Task<Dictionary<string, Customer>> GetCustomersByPhoneAsync()
        {
            var customers = await _context.Customers
                .Where(c => !string.IsNullOrEmpty(c.Phone))
                .ToListAsync();

            return customers
                .GroupBy(c => c.Phone.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        }

        private static string ResolveCustomerName(IEnumerable<Invoice> invoices, Dictionary<string, Customer> byPhone)
        {
            var latest = invoices.OrderByDescending(i => i.InvoiceDate).First();
            if (!string.IsNullOrWhiteSpace(latest.CustomerName))
                return latest.CustomerName.Trim();

            var phone = latest.CustomerPhone?.Trim();
            if (!string.IsNullOrEmpty(phone) && byPhone.TryGetValue(phone, out var customer)
                && !string.IsNullOrWhiteSpace(customer.FullName))
                return customer.FullName.Trim();

            if (!string.IsNullOrEmpty(phone))
                return $"Customer ({phone})";

            return "Walk-in Customer";
        }
    }
}
