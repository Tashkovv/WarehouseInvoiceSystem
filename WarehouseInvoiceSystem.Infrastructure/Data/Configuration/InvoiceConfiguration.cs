namespace WarehouseInvoiceSystem.Infrastructure.Data.Configuration
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;

    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>

    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            // Table name
            builder.ToTable("Invoice");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedOnAdd();
            builder.Property(e => e.CreatedAt).IsRequired();
            builder.Property(e => e.UpdatedAt);
            builder.Property(e => e.DeletedOn);

            builder.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(50);
            builder.Property(e => e.Type).IsRequired();
            builder.Property(e => e.Status).IsRequired().HasDefaultValue(InvoiceStatus.Draft);
            builder.Property(e => e.SubTotal).HasPrecision(18, 2).IsRequired();
            builder.Property(e => e.DiscountTotal).HasPrecision(18, 2).IsRequired().HasDefaultValue(0m);
            builder.Property(e => e.TaxAmount).HasPrecision(18, 2).IsRequired().HasDefaultValue(0);
            builder.Property(e => e.TotalAmount).HasPrecision(18, 2).IsRequired();
            builder.Property(e => e.AmountPaid).HasPrecision(18, 2).IsRequired().HasDefaultValue(0);
            builder.Property(e => e.IssueDate).IsRequired();
            builder.Property(e => e.DueDate).IsRequired();
            builder.Property(e => e.Notes).HasMaxLength(1000);
            builder.Property(e => e.CreatedBy).HasMaxLength(100);
            builder.Property(e => e.ModifiedBy).HasMaxLength(100);

            // Computed property - not stored in database
            builder.Ignore(e => e.AmountDue);

            // Indexes
            builder.HasIndex(e => new { e.Status, e.DueDate });
            builder.HasIndex(e => e.CompanyId);
            builder.HasIndex(e => e.WarehouseId);
            builder.HasIndex(e => e.IssueDate);
            builder.HasIndex(e => e.InvoiceNumber).IsUnique();
            builder.HasIndex(e => e.DeletedOn); 
            builder.HasIndex(e => e.Type);
            builder.HasIndex(e => new { e.Type, e.Status, e.DeletedOn });

            // Relationships
            builder.HasOne(e => e.Company)
                   .WithMany(c => c.Invoices)
                   .HasForeignKey(e => e.CompanyId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.Property(e => e.WarehouseId).IsRequired();
            builder.HasOne(e => e.Warehouse)
                   .WithMany()
                   .HasForeignKey(e => e.WarehouseId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
