namespace WarehouseInvoiceSystem.Infrastructure.Data.Configuration
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using WarehouseInvoiceSystem.Domain.Entities;

    public class IndividualConfiguration : IEntityTypeConfiguration<Individual>
    {
        public void Configure(EntityTypeBuilder<Individual> builder)
        {
            builder.ToTable("Individual");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedOnAdd();
            builder.Property(e => e.CreatedAt).IsRequired();
            builder.Property(e => e.UpdatedAt);
            builder.Property(e => e.DeletedOn);

            builder.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            builder.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            builder.Property(e => e.IdentificationNumber).IsRequired().HasMaxLength(50);
            builder.Property(e => e.Address).HasMaxLength(500);
            builder.Property(e => e.Phone).HasMaxLength(50);
            builder.Property(e => e.Email).HasMaxLength(100);
            builder.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

            // Ignore computed property
            builder.Ignore(e => e.FullName);

            // Indexes
            builder.HasIndex(e => e.IdentificationNumber);
            builder.HasIndex(e => e.LastName);
            builder.HasIndex(e => e.DeletedOn);
            builder.HasIndex(e => e.IsActive);
        }
    }
}