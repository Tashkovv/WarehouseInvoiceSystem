namespace WarehouseInvoiceSystem.Application.DTOs.AuditLog
{
    using WarehouseInvoiceSystem.Domain.Enums;

    public class AuditLogDto
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
