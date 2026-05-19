using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsBackend.Data;
using VehiclePartsBackend.Models;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;

namespace VehiclePartsBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = GetUserId();
            var cartItems = await _context.CartItems
                .Include(c => c.Part)
                .Where(c => c.AppUserId == userId)
                .Select(c => new {
                    id = c.Id,
                    partId = c.PartId,
                    name = c.Part.PartName,
                    brand = c.Part.Brand,
                    category = c.Part.Category,
                    price = c.Part.Price,
                    qty = c.Quantity,
                    stockQuantity = c.Part.StockQuantity
                })
                .ToListAsync();

            return Ok(cartItems);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] CartItemRequest request)
        {
            var userId = GetUserId();
            var part = await _context.Parts.FindAsync(request.PartId);
            if (part == null || part.StockQuantity < request.Quantity)
                return BadRequest("Invalid part or insufficient stock.");

            var existingItem = await _context.CartItems.FirstOrDefaultAsync(c => c.AppUserId == userId && c.PartId == request.PartId);
            if (existingItem != null)
            {
                existingItem.Quantity += request.Quantity;
                if (existingItem.Quantity > part.StockQuantity) existingItem.Quantity = part.StockQuantity;
            }
            else
            {
                _context.CartItems.Add(new CartItem {
                    AppUserId = userId,
                    PartId = request.PartId,
                    Quantity = request.Quantity
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Added to cart" });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCart(int id, [FromBody] CartItemRequest request)
        {
            var userId = GetUserId();
            var item = await _context.CartItems.Include(c => c.Part).FirstOrDefaultAsync(c => c.Id == id && c.AppUserId == userId);
            
            if (item == null) return NotFound();
            if (request.Quantity <= 0)
            {
                _context.CartItems.Remove(item);
            }
            else
            {
                if (request.Quantity > item.Part.StockQuantity) return BadRequest("Insufficient stock.");
                item.Quantity = request.Quantity;
            }

            await _context.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            var userId = GetUserId();
            var item = await _context.CartItems.FirstOrDefaultAsync(c => c.Id == id && c.AppUserId == userId);
            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }
            return Ok();
        }
    }

    public class CartItemRequest
    {
        public int PartId { get; set; }
        public int Quantity { get; set; }
    }
}
