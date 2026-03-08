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
        Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto createDto);
        Task<InvoiceDto> UpdateInvoiceAsync(Guid id, UpdateInvoiceDto updateDto);
        Task<bool> DeleteInvoiceAsync(Guid id);
        Task<InvoiceDto> UpdateInvoiceStatusAsync(Guid id, InvoiceStatus newStatus);
        Task<InvoiceSummaryDto> GetPayableInvoiceSummaryAsync();
        Task CreateInventoryTransactionsIfNeededAsync(Invoice invoice);
        Task CreateReverseTransactionsIfNeeded(Invoice invoice, string? reason = null);
    }
}
