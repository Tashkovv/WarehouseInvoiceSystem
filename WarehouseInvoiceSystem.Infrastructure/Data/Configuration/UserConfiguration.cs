namespace WarehouseInvoiceSystem.Infrastructure.Data.Configuration
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using WarehouseInvoiceSystem.Domain.Entities;

    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            // Table name
            builder.ToTable("User");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedOnAdd();
            builder.Property(e => e.CreatedAt).IsRequired();
            builder.Property(e => e.DeletedOn);

            builder.Property(e => e.Username).IsRequired().HasMaxLength(50);
            builder.Property(e => e.Email).IsRequired().HasMaxLength(100);
            builder.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            builder.Property(e => e.Role).IsRequired().HasMaxLength(50).HasDefaultValue("Viewer");
            builder.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

            // Indexes
            builder.HasIndex(e => e.Username).IsUnique();
            builder.HasIndex(e => e.Email).IsUnique();
            builder.HasIndex(e => e.IsActive);
            builder.HasIndex(e => e.DeletedOn);
        }
    }
}
