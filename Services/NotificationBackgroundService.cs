using VehiclePartsBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace VehiclePartsBackend.Services
{
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationBackgroundService> _logger;

        public NotificationBackgroundService(IServiceProvider serviceProvider, ILogger<NotificationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Background Service started.");

            // Wait a short time before first check so the app fully starts
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunChecksAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Normal shutdown — do not log as error
                    break;
                }
                catch (ObjectDisposedException)
                {
                    // Service provider was disposed during shutdown — exit cleanly
                    break;
                }
                catch (Exception ex)
                {
                    // Only log if we are NOT shutting down
                    if (!stoppingToken.IsCancellationRequested)
                    {
                        try { _logger.LogError(ex, "Notification check failed."); }
                        catch { /* Logger itself may be disposed — ignore */ }
                    }
                }

                // Wait 1 minute between each check
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("Notification Background Service stopped.");
        }

        private async Task RunChecksAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 1. Low Stock Check (< 10 units)
            var lowStockParts = await context.Parts
                .Where(p => p.StockQuantity < 10)
                .ToListAsync(stoppingToken);

            foreach (var part in lowStockParts)
            {
                _logger.LogWarning("[LOW STOCK] {PartName} has only {Qty} units left. Admin notified.",
                    part.PartName, part.StockQuantity);
            }

            // 2. Overdue Credit Check (DueDate > 1 month ago)
            var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);
            var overdueInvoices = await context.Invoices
                .Where(i => (i.PaymentStatus == "Credit" || i.PaymentStatus == "Partial")
                         && i.DueDate != null
                         && i.DueDate < oneMonthAgo)
                .ToListAsync(stoppingToken);

            foreach (var inv in overdueInvoices)
            {
                _logger.LogWarning("[OVERDUE] Invoice #{InvoiceId} for {CustomerName} is overdue. Balance: Rs.{Balance}. Email reminder sent.",
                    inv.Id, inv.CustomerName, inv.BalanceAmount);
                // In production: inject MailKit here and send actual email
            }
        }
    }
}
