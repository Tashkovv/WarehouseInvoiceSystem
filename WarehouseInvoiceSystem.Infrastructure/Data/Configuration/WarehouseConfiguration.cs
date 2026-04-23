namespace WarehouseInvoiceSystem.Infrastructure.Data.Configuration
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using WarehouseInvoiceSystem.Domain.Entities;

    public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
    {
        public void Configure(EntityTypeBuilder<Warehouse> builder)
        {
            // Table name
            builder.ToTable("Warehouse");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedOnAdd();
            builder.Property(e => e.CreatedAt).IsRequired();
            builder.Property(e => e.UpdatedAt);
            builder.Property(e => e.DeletedOn);

            builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
            builder.Property(e => e.Address).HasMaxLength(100);
            builder.Property(e => e.IsActive).IsRequired();
            builder.Property(e => e.IsDefault).IsRequired();

            builder.HasIndex(e => e.DeletedOn);
            builder.HasIndex(e => e.IsActive);
            builder.HasIndex(e => e.IsDefault);
        }
    }
}
