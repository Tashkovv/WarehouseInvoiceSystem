namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.Dashboard;
    using WarehouseInvoiceSystem.Application.DTOs.PurchaseNote;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Domain.Queries.Results;

    public interface IPurchaseNoteService
    {
        Task<IEnumerable<PurchaseNoteDto>> GetAllPurchaseNotesAsync(CancellationToken ct = default);
        Task<PagedResult<PurchaseNoteDto>> GetPagedAsync(GetPurchaseNotesQuery query, CancellationToken ct = default);
        Task<IEnumerable<PurchaseNoteDto>> GetAllFilteredAsync(GetPurchaseNotesQuery query, CancellationToken ct = default);
        Task<PurchaseNoteDto?> GetPurchaseNoteByIdAsync(Guid id, CancellationToken ct = default);
        Task<PurchaseNoteDto?> GetPurchaseNoteByNumberAsync(string noteNumber, CancellationToken ct = default);
        Task<IEnumerable<PurchaseNoteDto>> GetPurchaseNotesByIndividualAsync(Guid individualId, CancellationToken ct = default);
        Task<IEnumerable<PurchaseNoteDto>> GetPurchaseNotesByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);
        Task<IEnumerable<PurchaseNoteDto>> GetPurchaseNotesByStatusAsync(PurchaseNoteStatus status, CancellationToken ct = default);
        Task CreatePurchaseNoteAsync(CreatePurchaseNoteDto createDto);
        Task UpdatePurchaseNoteAsync(Guid id, UpdatePurchaseNoteDto updateDto);
        Task UpdateNotesAsync(Guid id, string? notes, CancellationToken ct = default);

        /// <summary>Receive goods: Draft → Pending. Creates inventory transactions.</summary>
        Task<PurchaseNoteDto> ReceiveAsync(Guid id);

        /// <summary>Settle payment: Pending → Paid. No stock change.</summary>
        Task<PurchaseNoteDto> MarkAsPaidAsync(Guid id);

        /// <summary>Revert to draft: Pending → Draft. Reverses inventory transactions.</summary>
        Task<PurchaseNoteDto> RevertToDraftAsync(Guid id);

        /// <summary>Cancel: Draft or Pending → Cancelled. Reverses stock if was Pending.</summary>
        Task<PurchaseNoteDto> CancelAsync(Guid id);

        /// <summary>Soft-deletes a Cancelled purchase note.</summary>
        Task<bool> DeletePurchaseNoteAsync(Guid id);

        // ── Dashboard targeted queries ────────────────────────────────────────────
        Task<IEnumerable<PurchaseNoteDto>> GetRecentAsync(int count, CancellationToken ct = default);
        Task<IEnumerable<PurchaseNoteDto>> GetByPurchaseDateAsync(DateTime date, CancellationToken ct = default);
        Task<IEnumerable<PurchaseNoteDto>> GetByPurchaseDateMonthAsync(int year, int month, CancellationToken ct = default);
        Task<(int unpaidCount, decimal unpaidAmount)> GetOutstandingPositionAsync(CancellationToken ct = default);
        Task<IEnumerable<PartnerSummaryDto>> GetTopVendorsBySpendAsync(DateTime from, DateTime to, int topCount, CancellationToken ct = default);
        Task<IEnumerable<PartnerAttentionDto>> GetUnpaidVendorSummariesAsync(CancellationToken ct = default);
        Task<IEnumerable<ProductMovementDto>> GetProductPurchasesByWarehouseAsync(Guid warehouseId, DateTime from, DateTime to, CancellationToken ct = default);

        // ── Dashboard aggregates ──────────────────────────────────────────────────
        Task<DayPurchaseNoteSummaryResult> GetDayPaidSummaryAsync(DateTime date, CancellationToken ct = default);
        Task<IEnumerable<PurchaseNoteDto>> GetTopUnpaidAsync(Guid? warehouseId, int top, CancellationToken ct = default);
        Task<IEnumerable<ProductMovementWithNameDto>> GetTopProductPurchasesAsync(Guid warehouseId, DateTime from, DateTime to, int top, CancellationToken ct = default);
        Task<DayPurchaseNoteSummaryResult> GetDayIssuedSummaryAsync(DateTime date, CancellationToken ct = default);
        Task<DayPurchaseNoteSummaryResult> GetMonthIssuedSummaryAsync(int year, int month, CancellationToken ct = default);
    }
}