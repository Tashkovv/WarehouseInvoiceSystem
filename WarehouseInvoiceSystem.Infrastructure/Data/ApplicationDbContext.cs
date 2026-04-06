namespace WarehouseInvoiceSystem.Infrastructure.Data
{
    using System.Text.Json;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.ChangeTracking;
    using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Common;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Queries.Results;
    using WarehouseInvoiceSystem.Infrastructure.Data.Configuration;

    public class ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IAuditContextService? auditContext = null) : DbContext(options)
    {
        public DbSet<Company> Companies { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceLine> InvoiceLines { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
        public DbSet<StockLevel> StockLevels { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Individual> Individuals { get; set; }
        public DbSet<PurchaseNote> PurchaseNotes { get; set; }
        public DbSet<PurchaseNoteLine> PurchaseNoteLines { get; set; }
        public DbSet<ProductPurchaseHistoryView> ProductPurchaseHistory { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationInvoice> NotificationInvoices { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all configurations from the current assembly
            modelBuilder.ApplyConfiguration(new CompanyConfiguration());
            modelBuilder.ApplyConfiguration(new InvoiceConfiguration());
            modelBuilder.ApplyConfiguration(new InvoiceLineConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentConfiguration());
            modelBuilder.ApplyConfiguration(new ProductConfiguration());
            modelBuilder.ApplyConfiguration(new WarehouseConfiguration());
            modelBuilder.ApplyConfiguration(new StockLevelConfiguration());
            modelBuilder.ApplyConfiguration(new InventoryTransactionConfiguration());
            modelBuilder.ApplyConfiguration(new IndividualConfiguration());
            modelBuilder.ApplyConfiguration(new PurchaseNoteConfiguration());
            modelBuilder.ApplyConfiguration(new PurchaseNoteLineConfiguration());
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new TenantConfiguration());
            modelBuilder.ApplyConfiguration(new NotificationConfiguration());
            modelBuilder.ApplyConfiguration(new NotificationInvoiceConfiguration());
            modelBuilder.ApplyConfiguration(new AuditLogConfiguration());

            modelBuilder.Entity<ProductPurchaseHistoryView>(e =>
            {
                e.HasNoKey();
                e.ToView("vw_product_purchase_history");
            });
        }

        // Entity types to audit (business entities only)
        private static readonly HashSet<string> _auditedTypes =
        [
            nameof(Company), nameof(Invoice), nameof(InvoiceLine), nameof(Payment),
            nameof(Product), nameof(Warehouse), nameof(InventoryTransaction),
            nameof(StockLevel), nameof(User), nameof(Individual),
            nameof(PurchaseNote), nameof(PurchaseNoteLine), nameof(Tenant)
        ];

        // Properties to skip (auto-set meta-fields that would create noise)
        private static readonly HashSet<string> _skipProperties =
        [
            nameof(AuditableEntity.CreatedAt),
            nameof(AuditableEntity.UpdatedAt),
            nameof(AuditableEntity.DeletedOn)
        ];

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            DateTime now = DateTime.UtcNow;
            string username = auditContext?.CurrentUsername ?? "UNKNOWN";

            // ── Set audit timestamps ──
            foreach (EntityEntry<AuditableEntity> entry in ChangeTracker.Entries<AuditableEntity>())
            {
                if (entry.State == EntityState.Added)
                    entry.Entity.CreatedAt = now;

                if (entry.State == EntityState.Modified)
                    entry.Entity.UpdatedAt = now;
            }

            // ── Capture audit entries before save ──
            List<AuditEntry> pendingAudits = [];

            foreach (EntityEntry<AuditableEntity> entry in ChangeTracker.Entries<AuditableEntity>())
            {
                string entityTypeName = entry.Entity.GetType().Name;
                if (!_auditedTypes.Contains(entityTypeName))
                    continue;

                if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;

                AuditEntry audit = new()
                {
                    EntityType = entityTypeName,
                    Username = username,
                    Timestamp = now
                };

                switch (entry.State)
                {
                    case EntityState.Added:
                        audit.Action = AuditAction.Created;
                        audit.EntityId = entry.Entity.Id; // May be empty for Added; captured after save
                        audit.NeedsIdAfterSave = entry.Entity.Id == Guid.Empty;
                        audit.Entry = entry;
                        foreach (PropertyEntry prop in entry.Properties)
                        {
                            if (_skipProperties.Contains(prop.Metadata.Name) || prop.Metadata.Name == "Id")
                                continue;
                            audit.Changes.Add(new()
                            {
                                Property = prop.Metadata.Name,
                                OldValue = null,
                                NewValue = prop.CurrentValue?.ToString()
                            });
                        }
                        break;

                    case EntityState.Modified:
                        // Detect soft delete: DeletedOn was null and is now set
                        bool isSoftDelete = entry.Property(nameof(AuditableEntity.DeletedOn)).IsModified
                                            && entry.Entity.DeletedOn is not null;

                        audit.Action = isSoftDelete ? AuditAction.Deleted : AuditAction.Updated;
                        audit.EntityId = entry.Entity.Id;

                        if (!isSoftDelete)
                        {
                            foreach (PropertyEntry prop in entry.Properties)
                            {
                                if (_skipProperties.Contains(prop.Metadata.Name) || !prop.IsModified)
                                    continue;
                                audit.Changes.Add(new()
                                {
                                    Property = prop.Metadata.Name,
                                    OldValue = prop.OriginalValue?.ToString(),
                                    NewValue = prop.CurrentValue?.ToString()
                                });
                            }
                        }
                        break;
                }

                if (audit.Action != default)
                    pendingAudits.Add(audit);
            }

            // ── Save business entities ──
            int result = await base.SaveChangesAsync(cancellationToken);

            // ── Write audit logs (capture generated IDs for Added entities) ──
            if (pendingAudits.Count > 0)
            {
                foreach (AuditEntry audit in pendingAudits)
                {
                    if (audit.NeedsIdAfterSave && audit.Entry is not null)
                        audit.EntityId = audit.Entry.Entity.Id;

                    AuditLogs.Add(new AuditLog
                    {
                        EntityType = audit.EntityType,
                        EntityId = audit.EntityId,
                        Action = audit.Action,
                        Changes = audit.Changes.Count > 0
                            ? JsonSerializer.Serialize(audit.Changes, _jsonOptions)
                            : null,
                        Username = audit.Username,
                        Timestamp = audit.Timestamp
                    });
                }

                await base.SaveChangesAsync(cancellationToken);
            }

            return result;
        }

        private sealed class AuditEntry
        {
            public string EntityType { get; set; } = string.Empty;
            public Guid EntityId { get; set; }
            public AuditAction Action { get; set; }
            public List<ChangeRecord> Changes { get; set; } = [];
            public string Username { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
            public bool NeedsIdAfterSave { get; set; }
            public EntityEntry<AuditableEntity>? Entry { get; set; }
        }

        private sealed class ChangeRecord
        {
            public string Property { get; set; } = string.Empty;
            public string? OldValue { get; set; }
            public string? NewValue { get; set; }
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);

            // Apply UTC converter to all DateTime properties
            configurationBuilder
                .Properties<DateTime>()
                .HaveConversion<UtcDateTimeConverter>();
        }

        public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
        {
            public UtcDateTimeConverter()
                : base(
                    v => v.Kind == DateTimeKind.Utc
                        ? v
                        : DateTime.SpecifyKind(v, DateTimeKind.Utc),
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
            {
            }
        }
    }
}