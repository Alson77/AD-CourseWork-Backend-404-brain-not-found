using System.Net.Mail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsBackend.Data;
using VehiclePartsBackend.Models;
using VehiclePartsBackend.Services;

namespace VehiclePartsBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Staff")]
    public class InvoicesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IInvoiceEmailService _emailService;

        public InvoicesController(AppDbContext context, IInvoiceEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _context.Invoices.Include(i => i.Items).ToListAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var invoice = await _context.Invoices.Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == id);
            if (invoice == null) return NotFound(new { message = $"Invoice {id} was not found." });
            return Ok(invoice);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Invoice invoice)
        {
            if (string.IsNullOrWhiteSpace(invoice.CustomerName))
                return BadRequest(new { message = "Customer name is required." });

            if (invoice.Items == null || invoice.Items.Count == 0)
                return BadRequest(new { message = "At least one item is required." });

            foreach (var item in invoice.Items)
            {
                if (string.IsNullOrWhiteSpace(item.PartName))
                    return BadRequest(new { message = "Each item must have a part name." });
                if (item.Quantity <= 0)
                    return BadRequest(new { message = "Item quantity must be at least 1." });
                if (item.UnitPrice <= 0)
                    return BadRequest(new { message = "Item unit price must be greater than zero." });
            }

            invoice.Subtotal = invoice.Items.Sum(i => i.Quantity * i.UnitPrice);
            
            // Loyalty Program: 10% discount if spend > 5000
            if (invoice.Subtotal > 5000)
            {
                var discount = invoice.Subtotal * 0.10m;
                if (invoice.Discount < discount) invoice.Discount = discount; // Apply at least 10%
            }

            if (invoice.Discount < 0) invoice.Discount = 0;
            if (invoice.Discount > invoice.Subtotal) invoice.Discount = invoice.Subtotal;

            invoice.Total = invoice.Subtotal - invoice.Discount;
            invoice.InvoiceDate = DateTime.UtcNow;
            
            // Set default payment logic if not provided
            if (string.IsNullOrEmpty(invoice.PaymentStatus))
                invoice.PaymentStatus = "Paid";

            if (invoice.PaymentStatus == "Paid")
            {
                invoice.PaidAmount = invoice.Total;
                invoice.BalanceAmount = 0;
            }
            else if (invoice.PaymentStatus == "Credit")
            {
                invoice.PaidAmount = 0;
                invoice.BalanceAmount = invoice.Total;
                invoice.DueDate = DateTime.UtcNow.AddDays(30); // 30 day credit term
            }
            else // Partial
            {
                invoice.BalanceAmount = invoice.Total - invoice.PaidAmount;
                if (invoice.BalanceAmount > 0) invoice.DueDate = DateTime.UtcNow.AddDays(30);
            }

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            // ── Decrement stock for each sold part ──────────────────────────
            foreach (var item in invoice.Items)
            {
                if (item.PartId.HasValue)
                {
                    var part = await _context.Parts.FindAsync(item.PartId.Value);
                    if (part != null)
                    {
                        part.StockQuantity = Math.Max(0, part.StockQuantity - item.Quantity);
                    }
                }
            }
            await _context.SaveChangesAsync();

            // Increase customer pending credit when sold on credit / partial
            if (invoice.BalanceAmount > 0)
            {
                var customer = await FindCustomerAsync(invoice.CustomerPhone, invoice.CustomerName);
                if (customer != null)
                    customer.PendingCredit += invoice.BalanceAmount;
                await _context.SaveChangesAsync();
            }

            return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
        }

        private async Task<Customer?> FindCustomerAsync(string phone, string name)
        {
            if (!string.IsNullOrWhiteSpace(phone))
            {
                var byPhone = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == phone);
                if (byPhone != null) return byPhone;
            }
            if (!string.IsNullOrWhiteSpace(name))
                return await _context.Customers.FirstOrDefaultAsync(c => c.FullName.ToLower() == name.ToLower());
            return null;
        }

        [HttpGet("email-configured")]
        public IActionResult EmailConfigured() => Ok(new { configured = _emailService.IsConfigured });

        [HttpPost("{id}/send-email")]
        public async Task<IActionResult> SendEmail(int id, [FromBody] SendInvoiceEmailDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest(new { message = "Customer email is required." });

            var invoice = await _context.Invoices.Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == id);
            if (invoice == null) return NotFound(new { message = $"Invoice {id} was not found." });

            try
            {
                await _emailService.SendInvoiceAsync(invoice, dto.Email.Trim());
                return Ok(new { message = $"Invoice #{invoice.Id} was sent to {dto.Email.Trim()}." });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(503, new { message = ex.Message, configured = false });
            }
            catch (SmtpException ex)
            {
                return BadRequest(new { message = $"SMTP error: {ex.Message}" });
            }
        }
    }

    public class SendInvoiceEmailDto
    {
        public string Email { get; set; } = string.Empty;
    }
}
