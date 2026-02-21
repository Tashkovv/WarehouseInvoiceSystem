namespace WarehouseInvoiceSystem.Infrastructure.Data.Configuration
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using WarehouseInvoiceSystem.Domain.Entities;

    public class PurchaseNoteLineConfiguration : IEntityTypeConfiguration<PurchaseNoteLine>
    {
        public void Configure(EntityTypeBuilder<PurchaseNoteLine> builder)
        {
            builder.ToTable("PurchaseNoteLine");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedOnAdd();
            builder.Property(e => e.CreatedAt).IsRequired();
            builder.Property(e => e.DeletedOn);

            builder.Property(e => e.Description).IsRequired().HasMaxLength(500);
            builder.Property(e => e.Quantity).HasPrecision(18, 2).IsRequired();
            builder.Property(e => e.UnitPrice).HasPrecision(18, 2).IsRequired();

            // Ignore computed property
            builder.Ignore(e => e.Amount);

            // Indexes
            builder.HasIndex(e => e.PurchaseNoteId);
            builder.HasIndex(e => e.ProductId);
            builder.HasIndex(e => e.DeletedOn);

            // Relationships
            builder.HasOne(e => e.PurchaseNote)
                   .WithMany(pn => pn.LineItems)
                   .HasForeignKey(e => e.PurchaseNoteId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Product)
                   .WithMany()
                   .HasForeignKey(e => e.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}