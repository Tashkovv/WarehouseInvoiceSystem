namespace WarehouseInvoiceSystem.Domain.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Domain.Queries.Results;

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

        // ── Dashboard targeted queries ────────────────────────────────────────────
        Task<IEnumerable<PurchaseNote>> GetRecentAsync(int count, CancellationToken ct = default);
        Task<IEnumerable<PurchaseNote>> GetByPurchaseDateAsync(DateTime date, CancellationToken ct = default);
        Task<IEnumerable<PurchaseNote>> GetByPurchaseDateMonthAsync(int year, int month, CancellationToken ct = default);
        Task<(int unpaidCount, decimal unpaidAmount)> GetOutstandingPositionAsync(CancellationToken ct = default);
        Task<IEnumerable<PartnerSummaryResult>> GetTopVendorsBySpendAsync(DateTime from, DateTime to, int topCount, CancellationToken ct = default);
        Task<IEnumerable<PartnerSummaryResult>> GetUnpaidVendorSummariesAsync(CancellationToken ct = default);
        Task<IEnumerable<ProductMovementResult>> GetProductPurchasesByWarehouseAsync(Guid warehouseId, DateTime from, DateTime to, CancellationToken ct = default);

        Task<IndividualAnalyticsResult> GetIndividualAnalyticsDataAsync(Guid individualId, CancellationToken ct = default);
    }
}