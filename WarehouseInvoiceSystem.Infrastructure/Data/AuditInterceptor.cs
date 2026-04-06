namespace WarehouseInvoiceSystem.Infrastructure.Data
{
    using System.Globalization;
    using System.Text.Json;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.ChangeTracking;
    using WarehouseInvoiceSystem.Domain.Common;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;

    /// <summary>
    /// Encapsulates all audit-log logic: timestamp setting, change capture, and persistence.
    /// Called from ApplicationDbContext.SaveChangesAsync via thin delegation.
    /// </summary>
    public sealed class AuditInterceptor
    {
        private static readonly HashSet<string> _auditedTypes =
        [
            nameof(Company), nameof(Invoice), nameof(InvoiceLine), nameof(Payment),
            nameof(Product), nameof(Warehouse), nameof(InventoryTransaction),
            nameof(StockLevel), nameof(User), nameof(Individual),
            nameof(PurchaseNote), nameof(PurchaseNoteLine), nameof(Tenant)
        ];

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

        public static async Task<int> ExecuteAsync(ApplicationDbContext context, CancellationToken cancellationToken)
        {
            DateTime now = DateTime.UtcNow;
            string username = context.CurrentUsername ?? "Unknown";

            SetAuditTimestamps(context, now);

            List<AuditEntry> pendingAudits = CaptureAuditEntries(context, username, now);

            int result = await context.BaseSaveChangesAsync(cancellationToken);

            if (pendingAudits.Count > 0)
                await WriteAuditLogs(context, pendingAudits, cancellationToken);

            return result;
        }

        private static void SetAuditTimestamps(ApplicationDbContext context, DateTime now)
        {
            foreach (EntityEntry<AuditableEntity> entry in context.ChangeTracker.Entries<AuditableEntity>())
            {
                if (entry.State == EntityState.Added)
                    entry.Entity.CreatedAt = now;

                if (entry.State == EntityState.Modified)
                    entry.Entity.UpdatedAt = now;
            }
        }

        private static List<AuditEntry> CaptureAuditEntries(
            ApplicationDbContext context, string username, DateTime timestamp)
        {
            List<AuditEntry> audits = [];

            foreach (EntityEntry<AuditableEntity> entry in context.ChangeTracker.Entries<AuditableEntity>())
            {
                string typeName = entry.Entity.GetType().Name;

                if (!_auditedTypes.Contains(typeName))
                    continue;

                if (entry.State is EntityState.Detached or EntityState.Unchanged)
                    continue;

                AuditEntry? audit = entry.State switch
                {
                    EntityState.Added => CaptureAdded(entry, typeName, username, timestamp),
                    EntityState.Modified => CaptureModified(entry, typeName, username, timestamp),
                    _ => null
                };

                if (audit is not null)
                    audits.Add(audit);
            }

            return audits;
        }

        private static AuditEntry CaptureAdded(
            EntityEntry<AuditableEntity> entry, string entityType, string username, DateTime timestamp)
        {
            AuditEntry audit = new()
            {
                EntityType = entityType,
                Action = AuditAction.Created,
                EntityId = entry.Entity.Id,
                NeedsIdAfterSave = entry.Entity.Id == Guid.Empty,
                TrackedEntry = entry,
                Username = username,
                Timestamp = timestamp
            };

            foreach (PropertyEntry prop in entry.Properties)
            {
                if (_skipProperties.Contains(prop.Metadata.Name) || prop.Metadata.Name == "Id")
                    continue;

                audit.Changes.Add(new ChangeRecord
                {
                    Property = prop.Metadata.Name,
                    OldValue = null,
                    NewValue = ToInvariantString(prop.CurrentValue)
                });
            }

            return audit;
        }

        private static AuditEntry? CaptureModified(
            EntityEntry<AuditableEntity> entry, string entityType, string username, DateTime timestamp)
        {
            bool isSoftDelete = entry.Property(nameof(AuditableEntity.DeletedOn)).IsModified
                                && entry.Entity.DeletedOn is not null;

            AuditEntry audit = new()
            {
                EntityType = entityType,
                Action = isSoftDelete ? AuditAction.Deleted : AuditAction.Updated,
                EntityId = entry.Entity.Id,
                Username = username,
                Timestamp = timestamp
            };

            if (!isSoftDelete)
            {
                foreach (PropertyEntry prop in entry.Properties)
                {
                    if (_skipProperties.Contains(prop.Metadata.Name) || !prop.IsModified)
                        continue;

                    audit.Changes.Add(new ChangeRecord
                    {
                        Property = prop.Metadata.Name,
                        OldValue = ToInvariantString(prop.OriginalValue),
                        NewValue = ToInvariantString(prop.CurrentValue)
                    });
                }
            }

            return audit.Action != default ? audit : null;
        }

        private static string? ToInvariantString(object? value) =>
            value is IFormattable f ? f.ToString(null, CultureInfo.InvariantCulture) : value?.ToString();

        private static async Task WriteAuditLogs(
            ApplicationDbContext context, List<AuditEntry> pendingAudits, CancellationToken cancellationToken)
        {
            foreach (AuditEntry audit in pendingAudits)
            {
                if (audit.NeedsIdAfterSave && audit.TrackedEntry is not null)
                    audit.EntityId = audit.TrackedEntry.Entity.Id;

                context.AuditLogs.Add(new AuditLog
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

            await context.BaseSaveChangesAsync(cancellationToken);
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
            public EntityEntry<AuditableEntity>? TrackedEntry { get; set; }
        }

        private sealed class ChangeRecord
        {
            public string Property { get; set; } = string.Empty;
            public string? OldValue { get; set; }
            public string? NewValue { get; set; }
        }
    }
}
