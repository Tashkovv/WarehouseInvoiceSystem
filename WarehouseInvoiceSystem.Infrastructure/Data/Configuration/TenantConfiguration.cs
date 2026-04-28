namespace WarehouseInvoiceSystem.Infrastructure.Data.Configuration
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using WarehouseInvoiceSystem.Domain.Entities;

    public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
    {
        public void Configure(EntityTypeBuilder<Tenant> builder)
        {
            builder.ToTable("Tenant");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedOnAdd();
            builder.Property(e => e.CreatedAt).IsRequired();
            builder.Property(e => e.UpdatedAt);
            builder.Property(e => e.DeletedOn);

            builder.Property(e => e.CompanyName).IsRequired().HasMaxLength(200);
            builder.Property(e => e.OperatorName).HasMaxLength(100);
            builder.Property(e => e.Address).HasMaxLength(500);
            builder.Property(e => e.Phone).HasMaxLength(50);
            builder.Property(e => e.TaxId).HasMaxLength(50);
            builder.Property(e => e.Embs).HasMaxLength(50);
            builder.Property(e => e.BankAccount).HasMaxLength(50);
            builder.Property(e => e.BankName).HasMaxLength(200);
            builder.Property(e => e.BankBranch).HasMaxLength(200);
            builder.Property(e => e.Email).HasMaxLength(100);
            builder.Property(e => e.EmailPasswordEncrypted).HasMaxLength(500);

            // bytea in PostgreSQL — no max length constraint
            builder.Property(e => e.LogoData).HasColumnType("bytea");
            builder.Property(e => e.LogoMimeType).HasMaxLength(50);

            // VAT / ДДВ
            builder.Property(e => e.VatRegistered).IsRequired().HasDefaultValue(false);
            builder.Property(e => e.VatPayerPeriod).IsRequired().HasDefaultValue(Domain.Enums.VatPayerPeriod.Quarterly);
            builder.Property(e => e.VatRegistrationDate);
        }
    }
}