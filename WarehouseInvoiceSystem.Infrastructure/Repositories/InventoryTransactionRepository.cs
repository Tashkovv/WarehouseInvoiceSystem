namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    public class InventoryTransactionRepository(IDbContextFactory<ApplicationDbContext> factory)
        : BaseRepository(factory), IInventoryTransactionRepository
    {
        public Task<IEnumerable<InventoryTransaction>> GetAllAsync() =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<InventoryTransaction>)await All<InventoryTransaction>(context)
                    .Include(t => t.Product)
                    .Include(t => t.Warehouse)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            });

        public Task<IEnumerable<InventoryTransaction>> GetByProductIdAsync(Guid productId) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<InventoryTransaction>)await All<InventoryTransaction>(context)
                    .Where(t => t.ProductId == productId)
                    .Include(t => t.Warehouse)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            });

        public Task<PagedResult<InventoryTransaction>> GetPagedByProductAsync(GetInventoryTransactionsQuery query) =>
            WithContextAsync(async context =>
            {
                IQueryable<InventoryTransaction> q = ApplyFilters(
                    All<InventoryTransaction>(context)
                        .Include(t => t.Warehouse),
                    query);

                q = q.OrderByDescending(t => t.CreatedAt);

                int totalCount = await q.CountAsync();

                List<InventoryTransaction> items = await q
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync();

                return new PagedResult<InventoryTransaction>
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = query.Page,
                    PageSize = query.PageSize
                };
            });

        public Task<IEnumerable<InventoryTransaction>> GetByWarehouseIdAsync(Guid warehouseId) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<InventoryTransaction>)await All<InventoryTransaction>(context)
                    .Where(t => t.WarehouseId == warehouseId)
                    .Include(t => t.Product)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            });

        public Task<IEnumerable<InventoryTransaction>> GetBySourceDocumentAsync(Guid sourceDocumentId, string sourceDocumentType) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<InventoryTransaction>)await All<InventoryTransaction>(context)
                    .Where(t => t.SourceDocumentId == sourceDocumentId &&
                                t.SourceDocumentType == sourceDocumentType)
                    .Include(t => t.Product)
                    .Include(t => t.Warehouse)
                    .ToListAsync();
            });

        public Task<InventoryTransaction?> GetByIdAsync(Guid id) =>
            WithContextAsync(context =>
                All<InventoryTransaction>(context)
                    .Include(t => t.Product)
                    .Include(t => t.Warehouse)
                    .FirstOrDefaultAsync(t => t.Id == id));

        public Task<bool> HasTransactionsForDocumentAsync(Guid sourceDocumentId, string sourceDocumentType) =>
            WithContextAsync(context =>
                All<InventoryTransaction>(context)
                    .AnyAsync(t => t.SourceDocumentId == sourceDocumentId &&
                                   t.SourceDocumentType == sourceDocumentType));

        public Task<IEnumerable<InventoryTransaction>> SoftDeleteReversalAsync(Guid sourceDocumentId, string sourceDocumentType) =>
            WithContextAsync(async context =>
            {
                string reversalType = $"{sourceDocumentType}_Reversal";

                List<InventoryTransaction> reversals = await AllTracked<InventoryTransaction>(context)
                    .Where(t => t.SourceDocumentId == sourceDocumentId &&
                                t.SourceDocumentType == reversalType)
                    .ToListAsync();

                foreach (InventoryTransaction reversal in reversals)
                    reversal.DeletedOn = DateTime.UtcNow;

                await SaveAsync(context);
                return (IEnumerable<InventoryTransaction>)reversals;
            });

        public Task<InventoryTransaction> CreateAsync(InventoryTransaction transaction) =>
            WithContextAsync(async context =>
            {
                transaction.CreatedAt = DateTime.UtcNow;
                context.InventoryTransactions.Add(transaction);
                await SaveAsync(context);
                return transaction;
            });

        private static IQueryable<InventoryTransaction> ApplyFilters(IQueryable<InventoryTransaction> q, GetInventoryTransactionsQuery query)
        {
            q = q.Where(t => t.ProductId == query.ProductId);

            if (query.WarehouseId.HasValue)
                q = q.Where(t => t.WarehouseId == query.WarehouseId.Value);

            if (query.Types is { Count: > 0 })
            {
                // Always include reversals regardless of type filter so the history stays coherent.
                // A reversal row has SourceDocumentType ending with "_Reversal".
                q = q.Where(t => query.Types.Contains(t.Type)
                               || (t.SourceDocumentType != null && t.SourceDocumentType.EndsWith("_Reversal")));
            }

            if (query.DateFrom.HasValue)
                q = q.Where(t => t.CreatedAt >= query.DateFrom.Value.Date);

            if (query.DateTo.HasValue)
                q = q.Where(t => t.CreatedAt < query.DateTo.Value.Date.AddDays(1));

            return q;
        }
    }
}