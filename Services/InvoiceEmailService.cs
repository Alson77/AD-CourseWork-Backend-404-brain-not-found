using System.Net;
using System.Net.Mail;
using System.Text;
using VehiclePartsBackend.Models;

namespace VehiclePartsBackend.Services
{
    public interface IInvoiceEmailService
    {
        bool IsConfigured { get; }
        Task SendInvoiceAsync(Invoice invoice, string toEmail);
    }

    public class InvoiceEmailService : IInvoiceEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<InvoiceEmailService> _logger;

        public InvoiceEmailService(IConfiguration configuration, ILogger<InvoiceEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public bool IsConfigured
        {
            get
            {
                var smtp = _configuration.GetSection("Smtp");
                return !string.IsNullOrWhiteSpace(smtp["Host"])
                    && !string.IsNullOrWhiteSpace(smtp["FromEmail"])
                    && !string.IsNullOrWhiteSpace(smtp["Username"])
                    && !string.IsNullOrWhiteSpace(smtp["Password"]);
            }
        }

        public async Task SendInvoiceAsync(Invoice invoice, string toEmail)
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException(
                    "Email is not configured. Set Smtp:Host, FromEmail, Username, and Password in appsettings.json (use a Gmail app password for Gmail).");
            }

            var smtp = _configuration.GetSection("Smtp");
            var host = smtp["Host"]!.Trim();
            var port = int.TryParse(smtp["Port"], out var p) ? p : 587;
            var enableSsl = !bool.TryParse(smtp["EnableSsl"], out var ssl) || ssl;
            var fromEmail = smtp["FromEmail"]!.Trim();
            var fromName = smtp["FromName"]?.Trim() ?? "Garage Hub";

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = $"Garage Hub Invoice #{invoice.Id}",
                Body = BuildHtmlBody(invoice),
                IsBodyHtml = true,
            };
            message.To.Add(toEmail.Trim());

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(smtp["Username"]!.Trim(), smtp["Password"] ?? ""),
            };

            await client.SendMailAsync(message);
            _logger.LogInformation("Invoice {InvoiceId} emailed to {Email}", invoice.Id, toEmail);
        }

        private static string BuildHtmlBody(Invoice invoice)
        {
            var sb = new StringBuilder();
            sb.Append("<html><body style=\"font-family:Arial,sans-serif;color:#222;\">");
            sb.Append("<h2 style=\"color:#c2410c;\">Garage Hub</h2>");
            sb.Append($"<p>Hello <strong>{WebUtility.HtmlEncode(invoice.CustomerName)}</strong>,</p>");
            sb.Append($"<p>Please find your invoice <strong>#{invoice.Id}</strong> dated {invoice.InvoiceDate:dd MMM yyyy}.</p>");
            sb.Append("<table border=\"1\" cellpadding=\"8\" cellspacing=\"0\" style=\"border-collapse:collapse;width:100%;max-width:600px;\">");
            sb.Append("<tr style=\"background:#f3f4f6;\"><th>Part</th><th>Qty</th><th>Unit</th><th>Line</th></tr>");
            foreach (var item in invoice.Items)
            {
                var line = item.Quantity * item.UnitPrice;
                sb.Append("<tr>");
                sb.Append($"<td>{WebUtility.HtmlEncode(item.PartName)}</td>");
                sb.Append($"<td>{item.Quantity}</td>");
                sb.Append($"<td>Rs. {item.UnitPrice:N2}</td>");
                sb.Append($"<td>Rs. {line:N2}</td>");
                sb.Append("</tr>");
            }
            sb.Append("</table>");
            sb.Append($"<p><strong>Subtotal:</strong> Rs. {invoice.Subtotal:N2}<br/>");
            if (invoice.Discount > 0)
                sb.Append($"<strong>Discount:</strong> Rs. {invoice.Discount:N2}<br/>");
            sb.Append($"<strong>Total:</strong> Rs. {invoice.Total:N2}</p>");
            if (invoice.BalanceAmount > 0)
                sb.Append($"<p style=\"color:#b45309;\"><strong>Balance due:</strong> Rs. {invoice.BalanceAmount:N2}</p>");
            sb.Append("<p>Thank you for your business.</p></body></html>");
            return sb.ToString();
        }
    }
}
