namespace WarehouseInvoiceSystem.Infrastructure.Data.Configuration
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using WarehouseInvoiceSystem.Domain.Entities;

    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notification");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedOnAdd();
            builder.Property(e => e.CreatedAt).IsRequired();
            builder.Property(e => e.UpdatedAt);
            builder.Property(e => e.DeletedOn);

            builder.Property(e => e.Type).IsRequired();
            builder.Property(e => e.Data);
            builder.Property(e => e.IsRead).IsRequired().HasDefaultValue(false);
            builder.Property(e => e.ReadAt);
            builder.Property(e => e.IsEmailSent).IsRequired().HasDefaultValue(false);
            builder.Property(e => e.EmailSentAt);

            // Indexes
            builder.HasIndex(e => new { e.IsRead, e.CreatedAt });
            builder.HasIndex(e => new { e.Data, e.CreatedAt });
            builder.HasIndex(e => e.DeletedOn);

            // Soft-delete query filter
            builder.HasQueryFilter(e => e.DeletedOn == null);
        }
    }
}
