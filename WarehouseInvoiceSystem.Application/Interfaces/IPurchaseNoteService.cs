namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.PurchaseNote;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

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

        /// <summary>Receive goods: Draft → Pending. Creates inventory transactions.</summary>
        Task<PurchaseNoteDto> ReceiveAsync(Guid id);

        /// <summary>Settle payment: Pending → Paid. No stock change.</summary>
        Task<PurchaseNoteDto> MarkAsPaidAsync(Guid id);

        /// <summary>Revert to draft: Pending → Draft. Reverses inventory transactions.</summary>
        Task<PurchaseNoteDto> RevertToDraftAsync(Guid id);

        /// <summary>Cancel: Draft or Pending → Cancelled. Reverses stock if was Pending.</summary>
        Task<PurchaseNoteDto> CancelAsync(Guid id);
    }
}