namespace WarehouseInvoiceSystem.Domain.Entities
{
    using WarehouseInvoiceSystem.Domain.Enums;

    /// <summary>
    /// Append-only audit trail entry. Not soft-deletable — permanent record.
    /// </summary>
    public class AuditLog
    {
        public Guid Id { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public AuditAction Action { get; set; }
        public string? Changes { get; set; }
        public string Username { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
