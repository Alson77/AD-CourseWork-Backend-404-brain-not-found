using Microsoft.AspNetCore.Mvc;
using VehiclePartsBackend.Models;

namespace VehiclePartsBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoicesController : ControllerBase
    {
        // ── In-memory store (runs without PostgreSQL) ──────────────────────
        private static int _nextId    = 1;
        private static int _nextItemId = 1;
        private static readonly List<Invoice> _invoices = new();

        // GET api/invoices
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_invoices);
        }

        // GET api/invoices/{id}
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var invoice = _invoices.FirstOrDefault(i => i.Id == id);
            if (invoice == null)
                return NotFound(new { message = $"Invoice {id} was not found." });
            return Ok(invoice);
        }

        // POST api/invoices
        [HttpPost]
        public IActionResult Create([FromBody] Invoice invoice)
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

            // Auto-calculate totals
            invoice.Subtotal = invoice.Items.Sum(i => i.Quantity * i.UnitPrice);

            if (invoice.Discount < 0)      invoice.Discount = 0;
            if (invoice.Discount > invoice.Subtotal) invoice.Discount = invoice.Subtotal;

            invoice.Total = invoice.Subtotal - invoice.Discount;

            // Assign IDs
            invoice.Id          = _nextId++;
            invoice.InvoiceDate = DateTime.UtcNow;

            foreach (var item in invoice.Items)
            {
                item.Id        = _nextItemId++;
                item.InvoiceId = invoice.Id;
            }

            _invoices.Add(invoice);
            return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
        }
    }
}
