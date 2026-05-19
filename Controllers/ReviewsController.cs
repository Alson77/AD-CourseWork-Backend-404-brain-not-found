using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsBackend.Data;
using VehiclePartsBackend.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace VehiclePartsBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReviewsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ReviewsController(AppDbContext context) { _context = context; }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _context.Reviews.Include(r => r.Customer).OrderByDescending(r => r.ReviewDate).ToListAsync());
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyReviews()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AppUserId == userId);
            if (customer == null) return Ok(new object[] {});

            var reviews = await _context.Reviews
                .Include(r => r.Customer)
                .Where(r => r.CustomerId == customer.Id)
                .OrderByDescending(r => r.ReviewDate)
                .ToListAsync();

            return Ok(reviews);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Review review)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AppUserId == userId);
            if (customer == null)
            {
                customer = new Customer
                {
                    AppUserId = userId,
                    FullName = User.FindFirstValue(ClaimTypes.Name) ?? "User",
                    Email = User.FindFirstValue(ClaimTypes.Email) ?? "",
                    Phone = "0000000000",
                    RegisteredDate = DateTime.UtcNow
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            review.CustomerId = customer.Id;
            review.ReviewDate = DateTime.UtcNow;
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
            return Ok(review);
        }
    }
}
