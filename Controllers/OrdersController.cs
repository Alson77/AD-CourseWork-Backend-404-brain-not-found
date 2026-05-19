using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsBackend.Data;
using VehiclePartsBackend.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace VehiclePartsBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout()
        {
            var userId = GetUserId();
            
            var cartItems = await _context.CartItems.Include(c => c.Part).Where(c => c.AppUserId == userId).ToListAsync();
            
            if (!cartItems.Any())
                return BadRequest("Cart is empty.");

            var subtotal = cartItems.Sum(c => c.Part.Price * c.Quantity);
            var discount = subtotal > 5000 ? Math.Round(subtotal * 0.10m, 2) : 0m;
            var totalAmount = subtotal - discount;

            var order = new Order
            {
                AppUserId = userId,
                OrderDate = DateTime.UtcNow,
                SubtotalAmount = subtotal,
                DiscountAmount = discount,
                TotalAmount = totalAmount,
                Status = "Completed",
                PaymentStatus = discount > 0 ? "Paid (Loyalty)" : "Paid"
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in cartItems)
            {
                _context.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    PartId = item.PartId,
                    PartName = item.Part.PartName,
                    Quantity = item.Quantity,
                    UnitPrice = item.Part.Price
                });

                // Reduce inventory
                var part = await _context.Parts.FindAsync(item.PartId);
                if (part != null)
                {
                    part.StockQuantity -= item.Quantity;
                    if (part.StockQuantity < 0) part.StockQuantity = 0;
                }
            }

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = discount > 0
                    ? $"Order placed! Gold loyalty discount of Rs. {discount:N2} applied."
                    : "Order placed successfully",
                orderId = order.Id,
                subtotal,
                discount,
                total = totalAmount
            });
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = GetUserId();
            var orders = await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.AppUserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return Ok(orders);
        }
    }
}
