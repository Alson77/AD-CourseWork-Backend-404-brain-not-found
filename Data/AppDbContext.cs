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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Set decimal precision for PostgreSQL
            modelBuilder.Entity<Part>().Property(p => p.Price)
                .HasColumnType("numeric(18,2)");

            modelBuilder.Entity<Invoice>().Property(i => i.Subtotal)
                .HasColumnType("numeric(18,2)");
            modelBuilder.Entity<Invoice>().Property(i => i.Discount)
                .HasColumnType("numeric(18,2)");
            modelBuilder.Entity<Invoice>().Property(i => i.Total)
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
        }
    }
}
