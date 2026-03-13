namespace WarehouseInvoiceSystem.Domain.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public interface IPurchaseNoteRepository
    {
        Task<IEnumerable<PurchaseNote>> GetAllAsync();
        Task<PagedResult<PurchaseNote>> GetPagedAsync(GetPurchaseNotesQuery query);
        Task<PurchaseNote?> GetByIdAsync(Guid id);
        Task<PurchaseNote?> GetByNoteNumberAsync(string noteNumber);
        Task<IEnumerable<PurchaseNote>> GetByIndividualIdAsync(Guid individualId);
        Task<IEnumerable<PurchaseNote>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<PurchaseNote>> GetByStatusAsync(PurchaseNoteStatus status);
        Task<IEnumerable<PurchaseNoteLine>> GetLineItemsByProductIdAsync(Guid productId);
        Task<PagedResult<PurchaseNoteLine>> GetPagedLineItemsByProductIdAsync(GetProductHistoryQuery query);
        Task<string> GenerateNoteNumberAsync();
        Task<bool> ExistsAsync(Guid id);
        Task CreateAsync(PurchaseNote purchaseNote);
        Task UpdateAsync(PurchaseNote purchaseNote);
        Task<bool> DeleteAsync(Guid id);
    }
}