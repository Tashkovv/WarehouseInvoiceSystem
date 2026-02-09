namespace WarehouseInvoiceSystem.Infrastructure.Data.Configuration
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using WarehouseInvoiceSystem.Domain.Payment.Domain;

    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            // Table name
            builder.ToTable("Payment");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedOnAdd();
            builder.Property(e => e.CreatedAt).IsRequired();
            builder.Property(e => e.DeletedOn);

            builder.Property(e => e.Amount).HasPrecision(18, 2).IsRequired();
            builder.Property(e => e.PaymentMethod).IsRequired();
            builder.Property(e => e.PaymentDate).IsRequired();
            builder.Property(e => e.ReferenceNumber).HasMaxLength(100);
            builder.Property(e => e.Notes).HasMaxLength(500);
            builder.Property(e => e.RecordedBy).HasMaxLength(100);

            // Indexes
            builder.HasIndex(e => e.PaymentDate);
            builder.HasIndex(e => e.InvoiceId);
            builder.HasIndex(e => e.ReferenceNumber);
            builder.HasIndex(e => e.DeletedOn);

            // Relationships
            builder.HasOne(e => e.Invoice)
                   .WithMany(i => i.Payments)
                   .HasForeignKey(e => e.InvoiceId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
