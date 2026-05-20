using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsBackend.Data;

namespace VehiclePartsBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("financial")]
        public async Task<IActionResult> GetFinancialReport([FromQuery] string type, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            // type: "daily", "monthly", "yearly", "custom"
            var query = _context.Invoices.AsQueryable();

            if (startDate.HasValue) query = query.Where(i => i.InvoiceDate >= startDate.Value);
            if (endDate.HasValue) query = query.Where(i => i.InvoiceDate <= endDate.Value);

            var invoices = await query.ToListAsync();
            var totalRevenue = invoices.Sum(i => i.Total);

            // Grouping
            object groupedData = null;
            if (type == "daily")
            {
                groupedData = invoices.GroupBy(i => i.InvoiceDate.Date)
                    .Select(g => new { Date = g.Key.ToString("yyyy-MM-dd"), Revenue = g.Sum(i => i.Total) })
                    .OrderBy(x => x.Date).ToList();
            }
            else if (type == "monthly")
            {
                groupedData = invoices.GroupBy(i => new { i.InvoiceDate.Year, i.InvoiceDate.Month })
                    .Select(g => new { Date = $"{g.Key.Year}-{g.Key.Month:D2}", Revenue = g.Sum(i => i.Total) })
                    .OrderBy(x => x.Date).ToList();
            }
            else if (type == "yearly")
            {
                groupedData = invoices.GroupBy(i => i.InvoiceDate.Year)
                    .Select(g => new { Date = g.Key.ToString(), Revenue = g.Sum(i => i.Total) })
                    .OrderBy(x => x.Date).ToList();
            }

            return Ok(new
            {
                TotalRevenue = totalRevenue,
                TotalInvoices = invoices.Count,
                Data = groupedData
            });
        }
    }
}
