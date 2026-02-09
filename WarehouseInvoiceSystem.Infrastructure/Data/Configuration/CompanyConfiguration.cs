namespace WarehouseInvoiceSystem.Infrastructure.Data.Configuration
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using WarehouseInvoiceSystem.Domain.Company.Domain;

    public class CompanyConfiguration : IEntityTypeConfiguration<Company>
    {
        public void Configure(EntityTypeBuilder<Company> builder)
        {
            // Table name
            builder.ToTable("Company");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

            builder.Property(e => e.Email)
                .HasMaxLength(100);

            builder.Property(e => e.Phone)
                .HasMaxLength(50);

            builder.Property(e => e.ContactPerson)
                .HasMaxLength(100);

            builder.Property(e => e.Address)
                .HasMaxLength(500);

            builder.Property(e => e.TaxId)
                .HasMaxLength(50);

            builder.Property(e => e.CreditLimit)
                .HasPrecision(18, 2);

            builder.Property(e => e.Type)
                .IsRequired();

            builder.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(e => e.CreatedAt)
                .IsRequired()
                .HasColumnType("timestamp without time zone");

            builder.Property(e => e.DeletedOn)
                .HasColumnType("timestamp without time zone");

            // Indexes
            builder.HasIndex(e => e.Name);
            builder.HasIndex(e => e.Type);
            builder.HasIndex(e => e.IsActive);
            builder.HasIndex(e => e.DeletedOn);
        }
    }
}
