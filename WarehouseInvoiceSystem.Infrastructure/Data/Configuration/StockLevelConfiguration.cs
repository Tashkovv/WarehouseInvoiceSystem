namespace WarehouseInvoiceSystem.Infrastructure.Data.Configuration
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using WarehouseInvoiceSystem.Domain.Entities;

    public class StockLevelConfiguration : IEntityTypeConfiguration<StockLevel>
    {
        public void Configure(EntityTypeBuilder<StockLevel> builder)
        {
            builder.ToTable("StockLevel");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedOnAdd();
            builder.Property(e => e.CreatedAt).IsRequired();
            builder.Property(e => e.DeletedOn);

            builder.Property(e => e.Quantity).HasPrecision(18, 2).IsRequired();
            builder.Property(e => e.ReservedQuantity).HasPrecision(18, 2).IsRequired().HasDefaultValue(0);
            builder.Property(e => e.MinimumQuantity).HasPrecision(18, 2);
            builder.Property(e => e.ReorderPoint).HasPrecision(18, 2);
            builder.Property(e => e.LastRestockedAt).IsRequired();

            builder.Ignore(e => e.AvailableQuantity);

            // Composite unique index - one stock level per product per warehouse
            builder.HasIndex(e => new { e.ProductId, e.WarehouseId }).IsUnique();
            builder.HasIndex(e => e.DeletedOn);

            builder.HasOne(e => e.Product)
                   .WithMany(p => p.StockLevels)
                   .HasForeignKey(e => e.ProductId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Warehouse)
                   .WithMany(w => w.StockLevels)
                   .HasForeignKey(e => e.WarehouseId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
