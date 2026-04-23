namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    public class AuditLogRepository(IDbContextFactory<ApplicationDbContext> factory) : IAuditLogRepository
    {
        public async Task<PagedResult<AuditLog>> GetPagedAsync(GetAuditLogsQuery query, CancellationToken ct = default)
        {
            await using var context = factory.CreateDbContext();
            ct.ThrowIfCancellationRequested();

            IQueryable<AuditLog> q = context.AuditLogs.AsNoTracking();

            // ── Filters ──
            if (!string.IsNullOrWhiteSpace(query.EntityType))
                q = q.Where(a => a.EntityType == query.EntityType);

            if (query.EntityId.HasValue)
                q = q.Where(a => a.EntityId == query.EntityId.Value);

            if (query.Action.HasValue)
                q = q.Where(a => a.Action == query.Action.Value);

            if (!string.IsNullOrWhiteSpace(query.Username))
                q = q.Where(a => a.Username == query.Username);

            if (query.DateFrom.HasValue)
                q = q.Where(a => a.Timestamp >= query.DateFrom.Value);

            if (query.DateTo.HasValue)
                q = q.Where(a => a.Timestamp <= query.DateTo.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                string search = query.Search.Trim().ToLower();
                q = q.Where(a =>
                    a.EntityType.ToLower().Contains(search) ||
                    a.Username.ToLower().Contains(search));
            }

            // ── Count ──
            int totalCount = await q.CountAsync(ct);

            // ── Sort ──
            q = query.SortBy?.ToLower() switch
            {
                "timestamp" => query.SortAscending ? q.OrderBy(a => a.Timestamp) : q.OrderByDescending(a => a.Timestamp),
                "entitytype" => query.SortAscending ? q.OrderBy(a => a.EntityType) : q.OrderByDescending(a => a.EntityType),
                "username" => query.SortAscending ? q.OrderBy(a => a.Username) : q.OrderByDescending(a => a.Username),
                "action" => query.SortAscending ? q.OrderBy(a => a.Action) : q.OrderByDescending(a => a.Action),
                _ => q.OrderByDescending(a => a.Timestamp)
            };

            // ── Page ──
            List<AuditLog> items = await q
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(ct);

            return new PagedResult<AuditLog>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        public async Task<IEnumerable<AuditLog>> GetEntityHistoryAsync(string entityType, Guid entityId, CancellationToken ct = default)
        {
            await using var context = factory.CreateDbContext();
            ct.ThrowIfCancellationRequested();

            return await context.AuditLogs
                .AsNoTracking()
                .Where(a => a.EntityType == entityType && a.EntityId == entityId)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<string>> GetDistinctEntityTypesAsync(CancellationToken ct = default)
        {
            await using var context = factory.CreateDbContext();
            ct.ThrowIfCancellationRequested();

            return await context.AuditLogs
                .AsNoTracking()
                .Select(a => a.EntityType)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync(ct);
        }
    }
}
