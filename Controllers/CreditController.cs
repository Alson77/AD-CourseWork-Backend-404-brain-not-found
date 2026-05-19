using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsBackend.Data;
using VehiclePartsBackend.Models;
using System.Security.Claims;

namespace VehiclePartsBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CreditController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CreditController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        [HttpGet("my")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMyCredit()
        {
            var userId = GetUserId();
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AppUserId == userId);

            if (customer == null)
            {
                return Ok(new
                {
                    pendingCredit = 0m,
                    customerName = "",
                    registeredDate = (DateTime?)null,
                    isOverdue = false,
                    isGoldMember = false,
                    loyaltyTotalSpent = 0m,
                    amountToGold = 5000m,
                    loyaltyProgress = 0m,
                    unpaidInvoices = Array.Empty<object>(),
                    recentOrders = Array.Empty<object>()
                });
            }

            var now = DateTime.UtcNow;
            var orders = await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.AppUserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var loyaltyFromOrders = orders.Sum(o =>
                o.SubtotalAmount > 0 ? o.SubtotalAmount : o.TotalAmount);

            var allInvoices = await _context.Invoices.ToListAsync();
            var myInvoices = allInvoices.Where(i => InvoiceBelongsToCustomer(i, customer)).ToList();

            foreach (var inv in myInvoices)
                NormalizeInvoiceBalance(inv);

            var unpaid = myInvoices.Where(i => i.BalanceAmount > 0).OrderByDescending(i => i.InvoiceDate).ToList();
            var pendingCredit = unpaid.Sum(i => i.BalanceAmount);
            var isOverdue = unpaid.Any(i => i.DueDate != null && i.DueDate < now);

            var loyaltyFromInvoices = myInvoices.Sum(i => i.Total);
            var loyaltyTotalSpent = loyaltyFromOrders + loyaltyFromInvoices;
            var isGoldMember = loyaltyTotalSpent >= 5000m;

            customer.PendingCredit = pendingCredit;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                pendingCredit,
                customerName = customer.FullName,
                registeredDate = customer.RegisteredDate,
                isOverdue,
                isGoldMember,
                loyaltyTotalSpent,
                amountToGold = Math.Max(0, 5000m - loyaltyTotalSpent),
                loyaltyProgress = Math.Min(100m, Math.Round(loyaltyTotalSpent / 5000m * 100m, 1)),
                unpaidInvoices = unpaid.Select(i => new
                {
                    i.Id,
                    i.InvoiceDate,
                    i.Total,
                    i.PaidAmount,
                    i.BalanceAmount,
                    i.PaymentStatus,
                    i.DueDate,
                    isOverdue = i.DueDate != null && i.DueDate < now
                }),
                recentOrders = orders.Take(8).Select(o => new
                {
                    o.Id,
                    o.OrderDate,
                    o.SubtotalAmount,
                    o.DiscountAmount,
                    o.TotalAmount,
                    o.PaymentStatus,
                    itemCount = o.Items?.Count ?? 0
                })
            });
        }

        private static bool InvoiceBelongsToCustomer(Invoice invoice, Customer customer)
        {
            if (!string.IsNullOrWhiteSpace(customer.Phone) &&
                !string.IsNullOrWhiteSpace(invoice.CustomerPhone) &&
                invoice.CustomerPhone.Trim().Equals(customer.Phone.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;

            if (!string.IsNullOrWhiteSpace(customer.FullName) &&
                !string.IsNullOrWhiteSpace(invoice.CustomerName) &&
                invoice.CustomerName.Trim().Equals(customer.FullName.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminOverview()
        {
            var now = DateTime.UtcNow;
            var allInvoices = await _context.Invoices.ToListAsync();

            var unpaidInvoices = new List<Invoice>();
            foreach (var inv in allInvoices)
            {
                NormalizeInvoiceBalance(inv);
                if (inv.BalanceAmount > 0)
                    unpaidInvoices.Add(inv);
            }

            var invoiceDtos = unpaidInvoices
                .OrderByDescending(i => i.InvoiceDate)
                .Select(i => new
                {
                    i.Id,
                    i.CustomerName,
                    i.CustomerPhone,
                    i.InvoiceDate,
                    i.Total,
                    i.PaidAmount,
                    i.BalanceAmount,
                    i.PaymentStatus,
                    i.DueDate,
                    IsOverdue = i.DueDate != null && i.DueDate < now
                })
                .ToList();

            var dbCustomers = await _context.Customers.ToListAsync();

            var customerGroups = unpaidInvoices
                .GroupBy(i => CustomerGroupKey(i.CustomerPhone, i.CustomerName))
                .Select(g =>
                {
                    var first = g.First();
                    var phone = first.CustomerPhone?.Trim() ?? "";
                    var name = first.CustomerName?.Trim() ?? "";
                    var matched = FindCustomerMatch(dbCustomers, phone, name);
                    var oldestDue = g.Where(i => i.DueDate != null).Select(i => i.DueDate).Min();
                    var isOverdue = g.Any(i => i.DueDate != null && i.DueDate < now);

                    return new
                    {
                        CustomerKey = g.Key,
                        Id = matched?.Id,
                        FullName = matched?.FullName ?? name,
                        Phone = !string.IsNullOrEmpty(matched?.Phone) ? matched.Phone : phone,
                        UnpaidInvoiceCount = g.Count(),
                        TotalOutstanding = g.Sum(i => i.BalanceAmount),
                        OldestDueDate = oldestDue,
                        Status = isOverdue ? "Overdue" : "Pending",
                        IsOverdue = isOverdue
                    };
                })
                .OrderByDescending(c => c.TotalOutstanding)
                .ToList();

            var totalOutstanding = unpaidInvoices.Sum(i => i.BalanceAmount);
            var overdueCount = unpaidInvoices.Count(i => i.DueDate != null && i.DueDate < now);

            return Ok(new
            {
                TotalOutstanding = totalOutstanding,
                PendingCustomerCount = customerGroups.Count,
                UnpaidInvoiceCount = unpaidInvoices.Count,
                OverdueCount = overdueCount,
                Customers = customerGroups,
                Invoices = invoiceDtos
            });
        }

        [HttpPost("admin/record-payment")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RecordPayment([FromBody] RecordPaymentDto dto)
        {
            if (dto.Amount <= 0)
                return BadRequest(new { message = "Payment amount must be greater than zero." });

            if (dto.InvoiceId.HasValue)
            {
                var invoice = await _context.Invoices.FindAsync(dto.InvoiceId.Value);
                if (invoice == null)
                    return NotFound(new { message = "Invoice not found." });

                if (dto.Amount > invoice.BalanceAmount)
                    return BadRequest(new { message = $"Payment exceeds invoice balance (Rs. {invoice.BalanceAmount:F2})." });

                ApplyPaymentToInvoice(invoice, dto.Amount);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Payment recorded.",
                    invoiceId = invoice.Id,
                    balanceRemaining = invoice.BalanceAmount
                });
            }

            if (!dto.CustomerId.HasValue &&
                string.IsNullOrWhiteSpace(dto.CustomerKey) &&
                string.IsNullOrWhiteSpace(dto.CustomerPhone) &&
                string.IsNullOrWhiteSpace(dto.CustomerName))
            {
                return BadRequest(new { message = "Provide invoiceId or customer identifiers (customerId, customerKey, phone, or name)." });
            }

            var unpaid = await GetUnpaidInvoicesForCustomerAsync(dto.CustomerId, dto.CustomerPhone, dto.CustomerName, dto.CustomerKey);
            if (!unpaid.Any())
                return NotFound(new { message = "No unpaid invoices found for this customer." });

            var totalOwed = unpaid.Sum(i => i.BalanceAmount);
            if (dto.Amount > totalOwed)
                return BadRequest(new { message = $"Payment exceeds outstanding balance (Rs. {totalOwed:F2})." });

            var remaining = dto.Amount;
            foreach (var inv in unpaid.OrderBy(i => i.InvoiceDate))
            {
                if (remaining <= 0) break;
                var apply = Math.Min(remaining, inv.BalanceAmount);
                ApplyPaymentToInvoice(inv, apply);
                remaining -= apply;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Payment recorded.",
                amountApplied = dto.Amount - remaining,
                balanceRemaining = unpaid.Sum(i => i.BalanceAmount)
            });
        }

        private static decimal EffectiveBalance(Invoice invoice) =>
            Math.Max(0, invoice.Total - invoice.PaidAmount);

        private static void NormalizeInvoiceBalance(Invoice invoice)
        {
            invoice.BalanceAmount = EffectiveBalance(invoice);
            if (invoice.BalanceAmount <= 0)
            {
                invoice.BalanceAmount = 0;
                invoice.PaidAmount = invoice.Total;
                invoice.PaymentStatus = "Paid";
            }
            else if (invoice.PaidAmount <= 0)
                invoice.PaymentStatus = "Credit";
            else
                invoice.PaymentStatus = "Partial";
        }

        private static void ApplyPaymentToInvoice(Invoice invoice, decimal amount)
        {
            invoice.PaidAmount += amount;
            invoice.BalanceAmount = Math.Max(0, invoice.Total - invoice.PaidAmount);
            invoice.PaymentStatus = invoice.BalanceAmount <= 0 ? "Paid" : "Partial";
            if (invoice.BalanceAmount <= 0)
                invoice.BalanceAmount = 0;
        }

        private static string CustomerGroupKey(string? phone, string? name)
        {
            var p = phone?.Trim().ToLowerInvariant() ?? "";
            var n = name?.Trim().ToLowerInvariant() ?? "";
            if (!string.IsNullOrEmpty(p)) return $"phone:{p}";
            return $"name:{n}";
        }

        private static Customer? FindCustomerMatch(List<Customer> customers, string phone, string name)
        {
            if (!string.IsNullOrWhiteSpace(phone))
            {
                var byPhone = customers.FirstOrDefault(c =>
                    c.Phone.Trim().Equals(phone, StringComparison.OrdinalIgnoreCase));
                if (byPhone != null) return byPhone;
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                return customers.FirstOrDefault(c =>
                    c.FullName.Trim().Equals(name, StringComparison.OrdinalIgnoreCase));
            }

            return null;
        }

        private async Task<List<Invoice>> GetUnpaidInvoicesForCustomerAsync(
            int? customerId, string? customerPhone, string? customerName, string? customerKey)
        {
            var all = await _context.Invoices.ToListAsync();
            var unpaid = new List<Invoice>();

            foreach (var inv in all)
            {
                NormalizeInvoiceBalance(inv);
                if (inv.BalanceAmount <= 0) continue;
                unpaid.Add(inv);
            }

            if (customerId.HasValue)
            {
                var customer = await _context.Customers.FindAsync(customerId.Value);
                if (customer == null) return new List<Invoice>();

                return unpaid
                    .Where(i =>
                        (!string.IsNullOrEmpty(customer.Phone) && i.CustomerPhone.Trim().Equals(customer.Phone.Trim(), StringComparison.OrdinalIgnoreCase)) ||
                        i.CustomerName.Trim().Equals(customer.FullName.Trim(), StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var key = !string.IsNullOrWhiteSpace(customerKey)
                ? customerKey
                : CustomerGroupKey(customerPhone, customerName);

            return unpaid.Where(i => CustomerGroupKey(i.CustomerPhone, i.CustomerName) == key).ToList();
        }

        public class RecordPaymentDto
        {
            public int? CustomerId { get; set; }
            public int? InvoiceId { get; set; }
            public decimal Amount { get; set; }
            public string? CustomerPhone { get; set; }
            public string? CustomerName { get; set; }
            public string? CustomerKey { get; set; }
        }
    }
}
