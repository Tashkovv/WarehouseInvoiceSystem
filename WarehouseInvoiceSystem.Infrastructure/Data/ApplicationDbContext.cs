namespace WarehouseInvoiceSystem.Infrastructure.Data
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Domain.Company.Domain;
    using WarehouseInvoiceSystem.Domain.Invoice.Domain;
    using WarehouseInvoiceSystem.Domain.InvoiceLine.Domain;
    using WarehouseInvoiceSystem.Domain.Payment.Domain;
    using WarehouseInvoiceSystem.Domain.User.Domain;
    using WarehouseInvoiceSystem.Infrastructure.Data.Configuration;

    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<Company> Companies { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceLine> InvoiceLines { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all configurations from the current assembly
            modelBuilder.ApplyConfiguration(new CompanyConfiguration());
            modelBuilder.ApplyConfiguration(new InvoiceConfiguration());
            modelBuilder.ApplyConfiguration(new InvoiceLineConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentConfiguration());
            modelBuilder.ApplyConfiguration(new UserConfiguration());
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // Configure all DateTime properties to use timestamp without time zone
            configurationBuilder
                .Properties<DateTime>()
                .HaveColumnType("timestamp without time zone");
        }
    }
}
