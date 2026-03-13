namespace WarehouseInvoiceSystem.Domain.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public interface IPurchaseNoteRepository
    {
        Task<IEnumerable<PurchaseNote>> GetAllAsync(CancellationToken ct = default);
        Task<PagedResult<PurchaseNote>> GetPagedAsync(GetPurchaseNotesQuery query, CancellationToken ct = default);

        Task<PurchaseNote?> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task<PurchaseNote?> GetByNoteNumberAsync(string noteNumber, CancellationToken ct = default);

        Task<IEnumerable<PurchaseNote>> GetByIndividualIdAsync(Guid individualId, CancellationToken ct = default);

        Task<IEnumerable<PurchaseNote>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);

        Task<IEnumerable<PurchaseNote>> GetByStatusAsync(PurchaseNoteStatus status, CancellationToken ct = default);

        Task<IEnumerable<PurchaseNoteLine>> GetLineItemsByProductIdAsync(Guid productId, CancellationToken ct = default);

        Task<PagedResult<PurchaseNoteLine>> GetPagedLineItemsByProductIdAsync(GetProductHistoryQuery query, CancellationToken ct = default);

        Task<string> GenerateNoteNumberAsync(CancellationToken ct = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

        Task CreateAsync(PurchaseNote purchaseNote);
        Task UpdateAsync(PurchaseNote purchaseNote);
        Task<bool> DeleteAsync(Guid id);
    }
}