namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.AuditLog;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public interface IAuditLogService
    {
        Task<PagedResult<AuditLogDto>> GetPagedAsync(GetAuditLogsQuery query, CancellationToken ct = default);
        Task<IEnumerable<AuditLogDto>> GetEntityHistoryAsync(string entityType, Guid entityId, CancellationToken ct = default);
        Task<IEnumerable<string>> GetAuditedEntityTypesAsync(CancellationToken ct = default);
    }
}
