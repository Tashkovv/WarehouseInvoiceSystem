namespace WarehouseInvoiceSystem.Infrastructure.Data.Configuration
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using WarehouseInvoiceSystem.Domain.Entities;

    public class NotificationInvoiceConfiguration : IEntityTypeConfiguration<NotificationInvoice>
    {
        public void Configure(EntityTypeBuilder<NotificationInvoice> builder)
        {
            builder.ToTable("NotificationInvoice");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedOnAdd();

            builder.HasOne(e => e.Notification)
                   .WithMany(n => n.NotificationInvoices)
                   .HasForeignKey(e => e.NotificationId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Invoice)
                   .WithMany()
                   .HasForeignKey(e => e.InvoiceId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(e => e.NotificationId);
            builder.HasIndex(e => e.InvoiceId);
            builder.HasIndex(e => new { e.NotificationId, e.InvoiceId }).IsUnique();
        }
    }
}
