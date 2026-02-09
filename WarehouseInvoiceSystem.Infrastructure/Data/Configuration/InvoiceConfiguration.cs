namespace WarehouseInvoiceSystem.Infrastructure.Data.Configuration
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using WarehouseInvoiceSystem.Domain.Invoice.Domain;
    using WarehouseInvoiceSystem.Domain.Invoice.Enums;

    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>

    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            // Table name
            builder.ToTable("Invoice");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.InvoiceNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(e => e.Type)
                .IsRequired();

            builder.Property(e => e.Status)
                .IsRequired()
                .HasDefaultValue(InvoiceStatus.Draft);

            builder.Property(e => e.SubTotal)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(e => e.TaxAmount)
                .HasPrecision(18, 2)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(e => e.TotalAmount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(e => e.AmountPaid)
                .HasPrecision(18, 2)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(e => e.IssueDate)
                .IsRequired()
                .HasColumnType("timestamp without time zone");

            builder.Property(e => e.DueDate)
                .IsRequired();

            builder.Property(e => e.Notes)
                .HasMaxLength(1000);

            builder.Property(e => e.CreatedAt)
                .IsRequired()
                .HasColumnType("timestamp without time zone");

            builder.Property(e => e.CreatedBy)
                .HasMaxLength(100);

            builder.Property(e => e.ModifiedBy)
                .HasMaxLength(100);

            // Computed property - not stored in database
            builder.Ignore(e => e.AmountDue);

            builder.HasIndex(e => new { e.Status, e.DueDate });

            builder.HasIndex(e => e.CompanyId);

            builder.HasIndex(e => e.IssueDate);
            
            builder.Property(e => e.DeletedOn)
                .HasColumnType("timestamp without time zone");

            // Indexes
            builder.HasIndex(e => e.InvoiceNumber)
                .IsUnique();
            builder.HasIndex(e => e.DeletedOn);

            // Relationships
            builder.HasOne(e => e.Company)
                .WithMany(c => c.Invoices)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
