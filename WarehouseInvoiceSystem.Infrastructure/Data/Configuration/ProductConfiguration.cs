namespace WarehouseInvoiceSystem.Infrastructure.Data.Configuration
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using WarehouseInvoiceSystem.Domain.Entities;

    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            // Table name
            builder.ToTable("Product");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedOnAdd();
            builder.Property(e => e.CreatedAt).IsRequired();
            builder.Property(e => e.UpdatedAt);
            builder.Property(e => e.DeletedOn);

            builder.Property(e => e.Code).IsRequired().HasMaxLength(50);
            builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
            builder.Property(e => e.Description).HasMaxLength(500);
            builder.Property(e => e.Unit).IsRequired().HasMaxLength(10);
            builder.Property(e => e.CostPrice).HasPrecision(18, 2).IsRequired().HasDefaultValue(0m);
            builder.Property(e => e.SellingPrice).HasPrecision(18, 2).IsRequired();
            builder.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

            // Indexes
            builder.HasIndex(e => e.Code).IsUnique();
            builder.HasIndex(e => e.IsActive);
            builder.HasIndex(e => e.DeletedOn);
            builder.HasIndex(e => e.Name);
        }
    }
}
