namespace WarehouseInvoiceSystem.Infrastructure.Data.Configuration
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using WarehouseInvoiceSystem.Domain.Entities;

    public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
    {
        public void Configure(EntityTypeBuilder<InvoiceLine> builder)
        {
            // Table name
            builder.ToTable("InvoiceLine");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(e => e.Quantity)
                .IsRequired();

            builder.Property(e => e.UnitPrice)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(e => e.TaxRate)
                .HasPrecision(5, 2)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(e => e.DeletedOn)
                .HasColumnType("timestamp without time zone");

            // Computed properties - not stored in database
            builder.Ignore(e => e.Amount);
            builder.Ignore(e => e.TaxAmount);
            builder.Ignore(e => e.TotalAmount);

            // Indexes
            builder.HasIndex(e => e.InvoiceId);
            builder.HasIndex(e => e.DeletedOn);

            // Relationships
            builder.HasOne(e => e.Invoice)
                .WithMany(i => i.LineItems)
                .HasForeignKey(e => e.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
