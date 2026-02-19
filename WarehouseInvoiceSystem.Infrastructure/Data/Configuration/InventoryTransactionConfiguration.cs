namespace WarehouseInvoiceSystem.Infrastructure.Data.Configuration
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using WarehouseInvoiceSystem.Domain.Entities;

    public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
    {
        public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
        {
            builder.ToTable("InventoryTransaction");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedOnAdd();
            builder.Property(e => e.CreatedAt).IsRequired();
            builder.Property(e => e.DeletedOn);

            builder.Property(e => e.Type).IsRequired();
            builder.Property(e => e.Quantity).HasPrecision(18, 2).IsRequired();
            builder.Property(e => e.SourceDocumentType).HasMaxLength(50);
            builder.Property(e => e.Note).HasMaxLength(500);

            builder.HasIndex(e => e.ProductId);
            builder.HasIndex(e => e.WarehouseId);
            builder.HasIndex(e => e.SourceDocumentId);
            builder.HasIndex(e => e.CreatedAt);
            builder.HasIndex(e => e.DeletedOn);

            builder.HasOne(e => e.Product)
                   .WithMany(p => p.InventoryTransactions)
                   .HasForeignKey(e => e.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Warehouse)
                   .WithMany(w => w.InventoryTransactions)
                   .HasForeignKey(e => e.WarehouseId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
