namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.Dashboard;
    using WarehouseInvoiceSystem.Application.DTOs.Invoice;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Domain.Queries.Results;

    public interface IInvoiceService
    {
        Task<IEnumerable<InvoiceDto>> GetAllInvoicesAsync(CancellationToken ct = default);
        Task<PagedResult<InvoiceDto>> GetPagedAsync(GetInvoicesQuery query, CancellationToken ct = default);
        Task<IEnumerable<InvoiceDto>> GetAllFilteredAsync(GetInvoicesQuery query, CancellationToken ct = default);
        Task<IEnumerable<InvoiceDto>> GetInvoicesByCompanyAsync(Guid companyId, CancellationToken ct = default);
        Task<IEnumerable<InvoiceDto>> GetInvoicesByTypeAsync(InvoiceType type, CancellationToken ct = default);
        Task<IEnumerable<InvoiceDto>> GetInvoicesByStatusAsync(InvoiceStatus status, CancellationToken ct = default);
        Task<IEnumerable<InvoiceDto>> GetOverdueInvoicesAsync(CancellationToken ct = default);
        Task<InvoiceDto?> GetInvoiceByIdAsync(Guid id, CancellationToken ct = default);
        Task<InvoiceDto?> GetInvoiceByNumberAsync(string invoiceNumber, CancellationToken ct = default);
        Task<Guid> CreateInvoiceAsync(CreateInvoiceDto createDto);
        Task UpdateInvoiceAsync(Guid id, UpdateInvoiceDto updateDto);
        Task UpdateNotesAsync(Guid id, string? notes, CancellationToken ct = default);
        Task<bool> DeleteInvoiceAsync(Guid id);

        // ── Status transitions ──────────────────────────────────────────────────

        /// <summary>Draft → Sent. Receivable only. Creates outbound inventory transactions.</summary>
        Task<InvoiceDto> SendAsync(Guid id);

        /// <summary>Sent/Overdue → Draft. Reverses inventory transactions. Not allowed for PartiallyPaid/Cancelled/Paid.</summary>
        Task<InvoiceDto> RevertToDraftAsync(Guid id);

        /// <summary>Sent/PartiallyPaid/Overdue → Paid. Creates inventory transactions if not yet done (covers Overdue Payable Draft edge case).</summary>
        Task<InvoiceDto> MarkAsPaidAsync(Guid id);

        /// <summary>Draft/Sent/Overdue → Cancelled. Reverses inventory transactions if any exist.</summary>
        Task<InvoiceDto> CancelAsync(Guid id);

        /// <summary>Any non-terminal status → Overdue. Called exclusively by BackgroundJobService. No inventory changes.</summary>
        Task<InvoiceDto> MarkAsOverdueAsync(Guid id);

        // ── Inventory helpers (used internally and by tests) ────────────────────

        Task CreateInventoryTransactionsIfNeededAsync(Invoice invoice);
        Task CreateReverseTransactionsIfNeeded(Invoice invoice, string? reason = null);
        Task<InvoiceSummaryDto> GetPayableInvoiceSummaryAsync(CancellationToken ct = default);

        // ── Dashboard targeted queries ────────────────────────────────────────────
        Task<IEnumerable<InvoiceDto>> GetRecentAsync(int count, CancellationToken ct = default);
        Task<IEnumerable<InvoiceDto>> GetByIssueDateAsync(DateTime date, CancellationToken ct = default);
        Task<IEnumerable<InvoiceDto>> GetByIssueDateMonthAsync(int year, int month, CancellationToken ct = default);
        Task<InvoiceOutstandingResult> GetOutstandingPositionAsync(CancellationToken ct = default);
        Task<IEnumerable<PartnerSummaryDto>> GetTopClientsByRevenueAsync(DateTime from, DateTime to, int topCount, CancellationToken ct = default);
        Task<IEnumerable<PartnerSummaryDto>> GetTopPayableVendorsBySpendAsync(DateTime from, DateTime to, int topCount, CancellationToken ct = default);
        Task<IEnumerable<PartnerAttentionDto>> GetOverdueClientSummariesAsync(CancellationToken ct = default);
        Task<IEnumerable<PartnerAttentionDto>> GetUnpaidPayableCompanySummariesAsync(CancellationToken ct = default);
        Task<IEnumerable<PartnerAttentionDto>> GetOverduePayableCompanySummariesAsync(CancellationToken ct = default);
        Task<IEnumerable<ProductMovementDto>> GetProductMovementByWarehouseAsync(Guid warehouseId, InvoiceType type, DateTime from, DateTime to, CancellationToken ct = default);

        // ── Dashboard aggregates ──────────────────────────────────────────────────
        Task<DayIssueSummaryResult> GetDayIssueSummaryAsync(DateTime date, CancellationToken ct = default);
        Task<IEnumerable<InvoiceDto>> GetTopOverdueReceivablesAsync(Guid? warehouseId, int top, CancellationToken ct = default);
        Task<IEnumerable<ProductMovementWithNameDto>> GetTopProductMovementAsync(Guid warehouseId, InvoiceType type, DateTime from, DateTime to, int top, CancellationToken ct = default);
        Task<InvoicePeriodSummaryResult> GetDayInvoiceSummaryAsync(DateTime date, CancellationToken ct = default);
        Task<InvoicePeriodSummaryResult> GetMonthInvoiceSummaryAsync(int year, int month, CancellationToken ct = default);
        Task<InvoicePeriodSummaryResult> GetYearInvoiceSummaryAsync(int year, CancellationToken ct = default);
    }
}