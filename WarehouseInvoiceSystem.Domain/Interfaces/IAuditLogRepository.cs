namespace WarehouseInvoiceSystem.Domain.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public interface IAuditLogRepository
    {
        Task<PagedResult<AuditLog>> GetPagedAsync(GetAuditLogsQuery query, CancellationToken ct = default);
        Task<IEnumerable<AuditLog>> GetEntityHistoryAsync(string entityType, Guid entityId, CancellationToken ct = default);
        Task<IEnumerable<string>> GetDistinctEntityTypesAsync(CancellationToken ct = default);
    }
}
