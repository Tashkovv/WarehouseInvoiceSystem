namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public class InventoryTransactionRepository(ApplicationDbContext context) : IInventoryTransactionRepository
    {
        public async Task<IEnumerable<InventoryTransaction>> GetAllAsync()
        {
            return await context.InventoryTransactions
                .Where(t => t.DeletedOn == null)
                .Include(t => t.Product)
                .Include(t => t.Warehouse)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<InventoryTransaction>> GetByProductIdAsync(Guid productId)
        {
            return await context.InventoryTransactions
                .Where(t => t.DeletedOn == null && t.ProductId == productId)
                .Include(t => t.Warehouse)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<PagedResult<InventoryTransaction>> GetPagedByProductAsync(GetInventoryTransactionsQuery query)
        {
            IQueryable<InventoryTransaction> q = context.InventoryTransactions
                .Where(t => t.DeletedOn == null && t.ProductId == query.ProductId)
                .Include(t => t.Warehouse);

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
        }

        public async Task<IEnumerable<InventoryTransaction>> GetByWarehouseIdAsync(Guid warehouseId)
        {
            return await context.InventoryTransactions
                .Where(t => t.DeletedOn == null && t.WarehouseId == warehouseId)
                .Include(t => t.Product)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<InventoryTransaction>> GetBySourceDocumentAsync(Guid sourceDocumentId, string sourceDocumentType)
        {
            return await context.InventoryTransactions
                .Where(t => t.DeletedOn == null &&
                            t.SourceDocumentId == sourceDocumentId &&
                            t.SourceDocumentType == sourceDocumentType)
                .Include(t => t.Product)
                .Include(t => t.Warehouse)
                .ToListAsync();
        }

        public async Task<InventoryTransaction?> GetByIdAsync(Guid id)
        {
            return await context.InventoryTransactions
                .Where(t => t.DeletedOn == null)
                .Include(t => t.Product)
                .Include(t => t.Warehouse)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<bool> HasTransactionsForDocumentAsync(Guid sourceDocumentId, string sourceDocumentType)
        {
            return await context.InventoryTransactions
                .AnyAsync(t => t.SourceDocumentId == sourceDocumentId &&
                               t.SourceDocumentType == sourceDocumentType &&
                               t.DeletedOn == null);
        }

        public async Task<IEnumerable<InventoryTransaction>> SoftDeleteReversalAsync(Guid sourceDocumentId, string sourceDocumentType)
        {
            string reversalType = $"{sourceDocumentType}_Reversal";

            List<InventoryTransaction> reversals = await context.InventoryTransactions
                .Where(t => t.DeletedOn == null &&
                            t.SourceDocumentId == sourceDocumentId &&
                            t.SourceDocumentType == reversalType)
                .ToListAsync();

            foreach (InventoryTransaction reversal in reversals)
                reversal.DeletedOn = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return reversals;
        }

        public async Task<InventoryTransaction> CreateAsync(InventoryTransaction transaction)
        {
            transaction.CreatedAt = DateTime.UtcNow;

            context.InventoryTransactions.Add(transaction);
            await context.SaveChangesAsync();

            return transaction;
        }
    }
}