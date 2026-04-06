namespace WarehouseInvoiceSystem.Infrastructure.Data.Configuration
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;

    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLog");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedOnAdd();

            builder.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
            builder.Property(e => e.EntityId).IsRequired();
            builder.Property(e => e.Action).IsRequired().HasConversion<string>();
            builder.Property(e => e.Changes).HasColumnType("jsonb");
            builder.Property(e => e.Username).IsRequired().HasMaxLength(100);
            builder.Property(e => e.Timestamp).IsRequired();

            // Indexes
            builder.HasIndex(e => new { e.EntityType, e.EntityId });
            builder.HasIndex(e => e.Timestamp);
            builder.HasIndex(e => e.Username);
        }
    }
}
