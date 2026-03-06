namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Infrastructure.Data;

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

        public async Task<InventoryTransaction> CreateAsync(InventoryTransaction transaction)
        {
            transaction.CreatedAt = DateTime.UtcNow;

            context.InventoryTransactions.Add(transaction);
            await context.SaveChangesAsync();

            return transaction;
        }
    }
}