namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.Invoice;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public interface IInvoiceService
    {
        Task<IEnumerable<InvoiceDto>> GetAllInvoicesAsync();
        Task<PagedResult<InvoiceDto>> GetPagedAsync(GetInvoicesQuery query);
        Task<IEnumerable<InvoiceDto>> GetAllFilteredAsync(GetInvoicesQuery query);
        Task<IEnumerable<InvoiceDto>> GetInvoicesByCompanyAsync(Guid companyId);
        Task<IEnumerable<InvoiceDto>> GetInvoicesByTypeAsync(InvoiceType type);
        Task<IEnumerable<InvoiceDto>> GetInvoicesByStatusAsync(InvoiceStatus status);
        Task<IEnumerable<InvoiceDto>> GetOverdueInvoicesAsync();
        Task<InvoiceDto?> GetInvoiceByIdAsync(Guid id);
        Task<InvoiceDto?> GetInvoiceByNumberAsync(string invoiceNumber);
        Task<Guid> CreateInvoiceAsync(CreateInvoiceDto createDto);
        Task UpdateInvoiceAsync(Guid id, UpdateInvoiceDto updateDto);
        Task<bool> DeleteInvoiceAsync(Guid id);

        // ── Status transitions ──────────────────────────────────────────────────

        /// <summary>Draft → Sent. Receivable only. Creates outbound inventory transactions.</summary>
        Task<InvoiceDto> SendAsync(Guid id);

        /// <summary>Sent/PartiallyPaid/Overdue → Paid. Creates inventory transactions if not yet done (covers Overdue Payable Draft edge case).</summary>
        Task<InvoiceDto> MarkAsPaidAsync(Guid id);

        /// <summary>Draft/Sent/Overdue → Cancelled. Reverses inventory transactions if any exist.</summary>
        Task<InvoiceDto> CancelAsync(Guid id);

        /// <summary>Any non-terminal status → Overdue. Called exclusively by BackgroundJobService. No inventory changes.</summary>
        Task<InvoiceDto> MarkAsOverdueAsync(Guid id);

        // ── Inventory helpers (used internally and by tests) ────────────────────

        Task CreateInventoryTransactionsIfNeededAsync(Invoice invoice);
        Task CreateReverseTransactionsIfNeeded(Invoice invoice, string? reason = null);
        Task<InvoiceSummaryDto> GetPayableInvoiceSummaryAsync();
    }
}