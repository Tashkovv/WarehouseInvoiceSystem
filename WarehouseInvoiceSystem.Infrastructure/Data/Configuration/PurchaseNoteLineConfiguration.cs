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
            builder.Property(e => e.UpdatedAt);
            builder.Property(e => e.DeletedOn);

            // Soft-delete filter — applied automatically on all queries including .Include()
            builder.HasQueryFilter(e => e.DeletedOn == null);

            builder.Property(e => e.Description).IsRequired().HasMaxLength(500);
            builder.Property(e => e.GrossQuantity).HasPrecision(18, 2).IsRequired();
            builder.Property(e => e.KaloPercentage).HasPrecision(5, 2).IsRequired();
            builder.Property(e => e.Quantity).HasPrecision(18, 2).IsRequired();
            builder.Property(e => e.UnitPrice).HasPrecision(18, 2).IsRequired();

            // Ignore computed property
            builder.Ignore(e => e.Amount);

            // Indexes
            builder.HasIndex(e => e.PurchaseNoteId);
            builder.HasIndex(e => e.ProductId);
            builder.HasIndex(e => e.DeletedOn);
            builder.HasIndex(e => new { e.ProductId, e.PurchaseNoteId })
                   .HasDatabaseName("IX_PurchaseNoteLine_ProductId_PurchaseNoteId");

            // Relationships
            builder.HasOne(e => e.PurchaseNote)
                   .WithMany(pn => pn.LineItems)
                   .HasForeignKey(e => e.PurchaseNoteId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Product)
                   .WithMany()
                   .HasForeignKey(e => e.ProductId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}