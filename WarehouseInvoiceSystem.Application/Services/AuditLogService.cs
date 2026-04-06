namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.DTOs.AuditLog;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public class AuditLogService(IAuditLogRepository repository) : IAuditLogService
    {
        public async Task<PagedResult<AuditLogDto>> GetPagedAsync(GetAuditLogsQuery query, CancellationToken ct = default)
        {
            PagedResult<AuditLog> result = await repository.GetPagedAsync(query, ct);

            return new PagedResult<AuditLogDto>
            {
                Items = result.Items.Select(MapToDto).ToList(),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<IEnumerable<AuditLogDto>> GetEntityHistoryAsync(string entityType, Guid entityId, CancellationToken ct = default)
        {
            IEnumerable<AuditLog> entries = await repository.GetEntityHistoryAsync(entityType, entityId, ct);
            return entries.Select(MapToDto);
        }

        public Task<IEnumerable<string>> GetAuditedEntityTypesAsync(CancellationToken ct = default)
            => repository.GetDistinctEntityTypesAsync(ct);

        private static AuditLogDto MapToDto(AuditLog entity) => new()
        {
            Id = entity.Id,
            EntityType = entity.EntityType,
            EntityId = entity.EntityId,
            Action = entity.Action,
            Changes = entity.Changes,
            Username = entity.Username,
            Timestamp = entity.Timestamp
        };
    }
}
