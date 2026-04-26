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
            builder.Property(e => e.Id).ValueGeneratedOnAdd();
            builder.Property(e => e.CreatedAt);
            builder.Property(e => e.UpdatedAt);
            builder.Property(e => e.DeletedOn);

            // Soft-delete filter — applied automatically on all queries including .Include()
            builder.HasQueryFilter(e => e.DeletedOn == null);

            builder.Property(e => e.Description).IsRequired().HasMaxLength(500);
            builder.Property(e => e.Quantity).IsRequired();
            builder.Property(e => e.UnitPrice).HasPrecision(18, 2).IsRequired();
            builder.Property(e => e.TaxRate).HasPrecision(5, 2).IsRequired().HasDefaultValue(0);
            builder.Property(e => e.DiscountPercentage).HasPrecision(5, 2).IsRequired().HasDefaultValue(0m);

            // Computed properties - not stored in database
            builder.Ignore(e => e.Amount);
            builder.Ignore(e => e.DiscountAmount);
            builder.Ignore(e => e.TaxAmount);
            builder.Ignore(e => e.TotalAmount);

            // Indexes
            builder.HasIndex(e => e.InvoiceId);
            builder.HasIndex(e => e.ProductId);
            builder.HasIndex(e => e.DeletedOn);
            builder.HasIndex(e => new { e.ProductId, e.InvoiceId })
                   .HasDatabaseName("IX_InvoiceLine_ProductId_InvoiceId");

            // Relationships
            builder.HasOne(e => e.Invoice)
                   .WithMany(i => i.LineItems)
                   .HasForeignKey(e => e.InvoiceId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Product)
                   .WithMany(p => p.InvoiceLines)
                   .HasForeignKey(e => e.ProductId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}