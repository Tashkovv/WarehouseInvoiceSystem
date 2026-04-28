namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    public class InventoryTransactionRepository(IDbContextFactory<ApplicationDbContext> factory, IAuditContextService auditContext)
        : BaseRepository(factory, auditContext), IInventoryTransactionRepository
    {
        public Task<IEnumerable<InventoryTransaction>> GetAllAsync(CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<InventoryTransaction>)await All<InventoryTransaction>(context)
                    .Include(t => t.Product)
                    .Include(t => t.Warehouse)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync(ct);
            });

        public Task<IEnumerable<InventoryTransaction>> GetByProductIdAsync(Guid productId, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<InventoryTransaction>)await All<InventoryTransaction>(context)
                    .Where(t => t.ProductId == productId)
                    .Include(t => t.Product)
                    .Include(t => t.Warehouse)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync(ct);
            });

        public Task<PagedResult<InventoryTransaction>> GetPagedByProductAsync(GetInventoryTransactionsQuery query, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                IQueryable<InventoryTransaction> q = ApplyFilters(
                    All<InventoryTransaction>(context)
                        .Include(t => t.Product)
                        .Include(t => t.Warehouse),
                    query);

                if (query.CompanyId.HasValue)
                {
                    IQueryable<Guid?> invoiceIds = All<Invoice>(context)
                        .Where(i => i.CompanyId == query.CompanyId.Value
                                 && i.Status != InvoiceStatus.Draft
                                 && i.Status != InvoiceStatus.Cancelled)
                        .Select(i => (Guid?)i.Id);
                    q = q.Where(t => t.SourceDocumentType != null
                                  && t.SourceDocumentType.StartsWith("Invoice")
                                  && invoiceIds.Contains(t.SourceDocumentId));
                }

                if (query.IndividualId.HasValue)
                {
                    IQueryable<Guid?> purchaseNoteIds = All<PurchaseNote>(context)
                        .Where(pn => pn.IndividualId == query.IndividualId.Value
                                  && pn.Status != PurchaseNoteStatus.Draft
                                  && pn.Status != PurchaseNoteStatus.Cancelled)
                        .Select(pn => (Guid?)pn.Id);
                    q = q.Where(t => t.SourceDocumentType != null
                                  && t.SourceDocumentType.StartsWith("PurchaseNote")
                                  && purchaseNoteIds.Contains(t.SourceDocumentId));
                }

                q = q.OrderByDescending(t => t.CreatedAt);

                int totalCount = await q.CountAsync(ct);

                List<InventoryTransaction> items = await q
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync(ct);

                return new PagedResult<InventoryTransaction>
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = query.Page,
                    PageSize = query.PageSize
                };
            });

        public Task<IEnumerable<InventoryTransaction>> GetByWarehouseIdAsync(Guid warehouseId, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<InventoryTransaction>)await All<InventoryTransaction>(context)
                    .Where(t => t.WarehouseId == warehouseId)
                    .Include(t => t.Product)
                    .Include(t => t.Warehouse)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync(ct);
            });

        public Task<IEnumerable<InventoryTransaction>> GetBySourceDocumentAsync(Guid sourceDocumentId, string sourceDocumentType, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<InventoryTransaction>)await All<InventoryTransaction>(context)
                    .Where(t => t.SourceDocumentId == sourceDocumentId &&
                                t.SourceDocumentType == sourceDocumentType)
                    .Include(t => t.Product)
                    .Include(t => t.Warehouse)
                    .ToListAsync(ct);
            });

        public Task<InventoryTransaction?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            WithContextAsync(context =>
                All<InventoryTransaction>(context)
                    .Include(t => t.Product)
                    .Include(t => t.Warehouse)
                    .FirstOrDefaultAsync(t => t.Id == id, ct));

        public Task<bool> HasTransactionsForDocumentAsync(Guid sourceDocumentId, string sourceDocumentType, CancellationToken ct = default) =>
            WithContextAsync(context =>
                All<InventoryTransaction>(context)
                    .AnyAsync(t => t.SourceDocumentId == sourceDocumentId &&
                                   t.SourceDocumentType == sourceDocumentType, ct));

        public Task<IEnumerable<InventoryTransaction>> SoftDeleteReversalAsync(Guid sourceDocumentId, string sourceDocumentType, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                string reversalType = $"{sourceDocumentType}_Reversal";

                List<InventoryTransaction> reversals = await AllTracked<InventoryTransaction>(context)
                    .Where(t => t.SourceDocumentId == sourceDocumentId &&
                                t.SourceDocumentType == reversalType)
                    .ToListAsync(ct);

                foreach (InventoryTransaction reversal in reversals)
                    reversal.DeletedOn = DateTime.UtcNow;

                await SaveAsync(context);
                return (IEnumerable<InventoryTransaction>)reversals;
            });

        public Task<IEnumerable<InventoryTransaction>> SoftDeleteByDocumentAsync(Guid sourceDocumentId, string sourceDocumentType, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                List<InventoryTransaction> transactions = await AllTracked<InventoryTransaction>(context)
                    .Where(t => t.SourceDocumentId == sourceDocumentId &&
                                t.SourceDocumentType == sourceDocumentType)
                    .ToListAsync(ct);

                foreach (InventoryTransaction t in transactions)
                    t.DeletedOn = DateTime.UtcNow;

                await SaveAsync(context);
                return (IEnumerable<InventoryTransaction>)transactions;
            });

        public Task<InventoryTransaction> CreateAsync(InventoryTransaction transaction) =>
            WithContextAsync(async context =>
            {
                transaction.CreatedAt = DateTime.UtcNow;
                context.InventoryTransactions.Add(transaction);
                await SaveAsync(context);
                return transaction;
            });

        public Task<(decimal TotalIncoming, decimal TotalOutgoing)> GetMovementTotalsAsync(
            Guid productId, Guid? warehouseId, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                IQueryable<InventoryTransaction> baseQ = All<InventoryTransaction>(context)
                    .Where(t => t.ProductId == productId
                             && (!warehouseId.HasValue || t.WarehouseId == warehouseId.Value));

                decimal incoming = await baseQ
                    .Where(t => t.Type == InventoryTransactionType.Inbound
                             || t.Type == InventoryTransactionType.TransferIn
                             || ((t.Type == InventoryTransactionType.Adjustment
                                  || t.Type == InventoryTransactionType.Reversed)
                                 && t.Quantity > 0))
                    .SumAsync(t => t.Quantity, ct);

                decimal outgoing = await baseQ
                    .Where(t => t.Type == InventoryTransactionType.Outbound
                             || t.Type == InventoryTransactionType.TransferOut
                             || ((t.Type == InventoryTransactionType.Adjustment
                                  || t.Type == InventoryTransactionType.Reversed)
                                 && t.Quantity < 0))
                    .SumAsync(t => Math.Abs(t.Quantity), ct);

                return (incoming, outgoing);
            });

        public Task<IEnumerable<InventoryTransaction>> GetTopRecentByWarehouseAsync(
            Guid warehouseId, int top, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<InventoryTransaction>)await All<InventoryTransaction>(context)
                    .Where(t => t.WarehouseId == warehouseId)
                    .Include(t => t.Product)
                    .Include(t => t.Warehouse)
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(top)
                    .ToListAsync(ct);
            });

        public Task CreateBatchAsync(IEnumerable<InventoryTransaction> transactions) =>
            WithContextAsync(async context =>
            {
                DateTime now = DateTime.UtcNow;
                foreach (InventoryTransaction t in transactions)
                    t.CreatedAt = now;

                context.InventoryTransactions.AddRange(transactions);
                await SaveAsync(context);
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