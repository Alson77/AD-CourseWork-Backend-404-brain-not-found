using Microsoft.EntityFrameworkCore;
using VehiclePartsBackend.Models;

namespace VehiclePartsBackend.Data
{
    // ── AppDbContext ──────────────────────────────────────────────────────────
    // When PostgreSQL is ready:
    //   1. Set "ConnectionStrings:DefaultConnection" in appsettings.json
    //   2. Register in Program.cs:
    //        builder.Services.AddDbContext<AppDbContext>(opt =>
    //            opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
    //   3. Run migrations:
    //        dotnet ef migrations add InitialCreate
    //        dotnet ef database update
    // ─────────────────────────────────────────────────────────────────────────
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Part> Parts { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<CustomerVehicle> CustomerVehicles { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<AppUser> Users { get; set; }
        public DbSet<Vendor> Vendors { get; set; }
        public DbSet<PurchaseInvoice> PurchaseInvoices { get; set; }
        public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<PartRequest> PartRequests { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<CreditAccount> CreditAccounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>().Property(c => c.PendingCredit)
                .HasColumnType("numeric(18,2)");
            // Set decimal precision for PostgreSQL
            modelBuilder.Entity<Part>().Property(p => p.Price)
                .HasColumnType("numeric(18,2)");

            modelBuilder.Entity<Order>().Property(o => o.TotalAmount)
                .HasColumnType("numeric(18,2)");
            modelBuilder.Entity<OrderItem>().Property(oi => oi.UnitPrice)
                .HasColumnType("numeric(18,2)");

            modelBuilder.Entity<CreditAccount>().Property(c => c.CreditLimit)
                .HasColumnType("numeric(18,2)");
            modelBuilder.Entity<CreditAccount>().Property(c => c.UsedCredit)
                .HasColumnType("numeric(18,2)");
            modelBuilder.Entity<CreditAccount>().Property(c => c.DueAmount)
                .HasColumnType("numeric(18,2)");

            modelBuilder.Entity<Invoice>().Property(i => i.Subtotal)
                .HasColumnType("numeric(18,2)");
            modelBuilder.Entity<Invoice>().Property(i => i.Discount)
                .HasColumnType("numeric(18,2)");
            modelBuilder.Entity<Invoice>().Property(i => i.Total)
                .HasColumnType("numeric(18,2)");
            modelBuilder.Entity<Invoice>().Property(i => i.PaidAmount)
                .HasColumnType("numeric(18,2)");
            modelBuilder.Entity<Invoice>().Property(i => i.BalanceAmount)
                .HasColumnType("numeric(18,2)");

            modelBuilder.Entity<InvoiceItem>().Property(ii => ii.UnitPrice)
                .HasColumnType("numeric(18,2)");

            // LineTotal is computed — ignore in DB
            modelBuilder.Entity<InvoiceItem>().Ignore(ii => ii.LineTotal);

            // Invoice → InvoiceItems relationship
            modelBuilder.Entity<InvoiceItem>()
                .HasOne<Invoice>()
                .WithMany(i => i.Items)
                .HasForeignKey(ii => ii.InvoiceId);

            // PurchaseInvoice settings
            modelBuilder.Entity<PurchaseInvoice>().Property(i => i.TotalAmount)
                .HasColumnType("numeric(18,2)");

            modelBuilder.Entity<PurchaseInvoiceItem>().Property(ii => ii.CostPrice)
                .HasColumnType("numeric(18,2)");
            
            modelBuilder.Entity<PurchaseInvoiceItem>().Ignore(ii => ii.LineTotal);

            modelBuilder.Entity<PurchaseInvoiceItem>()
                .HasOne<PurchaseInvoice>()
                .WithMany(i => i.Items)
                .HasForeignKey(ii => ii.PurchaseInvoiceId);

            // Seed initial admin user so we don't get locked out
            modelBuilder.Entity<AppUser>().HasData(
                new AppUser { Id = 1, Name = "Admin User", Email = "admin@garagehub.com", Password = "Admin@123", Role = "Admin", IsActive = true },
                new AppUser { Id = 2, Name = "John Customer", Email = "customer@garagehub.com", Password = "Customer@123", Role = "Customer", IsActive = true }
            );
        }
    }
}
