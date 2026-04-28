namespace WarehouseInvoiceSystem.Infrastructure.Data.Configuration
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using WarehouseInvoiceSystem.Domain.Entities;

    public class PurchaseNoteConfiguration : IEntityTypeConfiguration<PurchaseNote>
    {
        public void Configure(EntityTypeBuilder<PurchaseNote> builder)
        {
            builder.ToTable("PurchaseNote");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedOnAdd();
            builder.Property(e => e.CreatedAt).IsRequired();
            builder.Property(e => e.UpdatedAt);
            builder.Property(e => e.DeletedOn);

            builder.Property(e => e.NoteNumber).IsRequired().HasMaxLength(50);
            builder.Property(e => e.PurchaseDate).IsRequired();
            builder.Property(e => e.SubTotal).HasPrecision(18, 2).IsRequired();
            builder.Property(e => e.TotalAmount).HasPrecision(18, 2).IsRequired();
            builder.Property(e => e.Status).IsRequired();
            builder.Property(e => e.PaidDate);
            builder.Property(e => e.Notes).HasMaxLength(1000);

            // Indexes
            builder.HasIndex(e => e.NoteNumber).IsUnique();
            builder.HasIndex(e => e.IndividualId);
            builder.HasIndex(e => e.WarehouseId);
            builder.HasIndex(e => e.PurchaseDate);
            builder.HasIndex(e => e.Status);
            builder.HasIndex(e => e.DeletedOn);
            builder.HasIndex(e => new { e.Status, e.DeletedOn, e.PurchaseDate });

            // Relationships
            builder.HasOne(e => e.Individual)
                   .WithMany(i => i.PurchaseNotes)
                   .HasForeignKey(e => e.IndividualId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Warehouse)
                   .WithMany()
                   .HasForeignKey(e => e.WarehouseId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}