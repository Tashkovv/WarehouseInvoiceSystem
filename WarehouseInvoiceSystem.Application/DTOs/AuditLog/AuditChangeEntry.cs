namespace WarehouseInvoiceSystem.Application.DTOs.AuditLog
{
    public class AuditChangeEntry
    {
        public string Property { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }
}
