using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsBackend.Data;
using VehiclePartsBackend.Models;

namespace VehiclePartsBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class PurchasesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PurchasesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetPurchases()
        {
            var purchases = await _context.PurchaseInvoices
                .Include(p => p.Vendor)
                .Include(p => p.Items)
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();
            return Ok(purchases);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePurchase([FromBody] PurchaseDto dto)
        {
            if (dto.Items == null || !dto.Items.Any())
                return BadRequest(new { message = "No items in purchase invoice." });

            var vendor = await _context.Vendors.FindAsync(dto.VendorId);
            if (vendor == null)
                return BadRequest(new { message = "Vendor not found." });

            var invoice = new PurchaseInvoice
            {
                VendorId = dto.VendorId,
                PurchaseDate = DateTime.UtcNow,
                TotalAmount = 0
            };

            decimal totalAmount = 0;
            var invoiceItems = new List<PurchaseInvoiceItem>();

            foreach (var itemDto in dto.Items)
            {
                var part = await _context.Parts.FindAsync(itemDto.PartId);
                if (part == null)
                    return BadRequest(new { message = $"Part with ID {itemDto.PartId} not found." });

                totalAmount += itemDto.Quantity * itemDto.CostPrice;
                part.StockQuantity += itemDto.Quantity;

                invoiceItems.Add(new PurchaseInvoiceItem
                {
                    PartId = itemDto.PartId,
                    Quantity = itemDto.Quantity,
                    CostPrice = itemDto.CostPrice
                });
            }

            invoice.TotalAmount = totalAmount;
            _context.PurchaseInvoices.Add(invoice);
            await _context.SaveChangesAsync();

            // Now link items to the saved invoice
            foreach (var item in invoiceItems)
            {
                item.PurchaseInvoiceId = invoice.Id;
                _context.PurchaseInvoiceItems.Add(item);
            }
            await _context.SaveChangesAsync();

            return Ok(new { 
                invoice.Id,
                invoice.VendorId,
                invoice.PurchaseDate,
                invoice.TotalAmount,
                ItemCount = invoiceItems.Count
            });
        }
    }

    // DTO to avoid EF Core navigation property conflicts
    public class PurchaseDto
    {
        public int VendorId { get; set; }
        public List<PurchaseItemDto> Items { get; set; } = new();
    }

    public class PurchaseItemDto
    {
        public int PartId { get; set; }
        public int Quantity { get; set; }
        public decimal CostPrice { get; set; }
    }
}
