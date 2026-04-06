namespace WarehouseInvoiceSystem.Domain.Queries
{
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public class GetAuditLogsQuery : PagedQuery
    {
        public string? EntityType { get; set; }
        public Guid? EntityId { get; set; }
        public AuditAction? Action { get; set; }
        public string? Username { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}
