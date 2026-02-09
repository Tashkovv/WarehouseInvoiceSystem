namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.Invoice;
    using WarehouseInvoiceSystem.Domain.Invoice.Enums;

    public interface IInvoiceService
    {
        Task<IEnumerable<InvoiceDto>> GetAllInvoicesAsync();
        Task<IEnumerable<InvoiceDto>> GetInvoicesByCompanyAsync(int companyId);
        Task<IEnumerable<InvoiceDto>> GetInvoicesByTypeAsync(InvoiceType type);
        Task<IEnumerable<InvoiceDto>> GetInvoicesByStatusAsync(InvoiceStatus status);
        Task<IEnumerable<InvoiceDto>> GetOverdueInvoicesAsync();
        Task<InvoiceDto?> GetInvoiceByIdAsync(int id);
        Task<InvoiceDto?> GetInvoiceByNumberAsync(string invoiceNumber);
        Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto createDto);
        Task<InvoiceDto> UpdateInvoiceAsync(int id, UpdateInvoiceDto updateDto);
        Task<bool> DeleteInvoiceAsync(int id);
        Task<InvoiceDto> UpdateInvoiceStatusAsync(int id, InvoiceStatus status);
        Task<InvoiceSummaryDto> GetInvoiceSummaryAsync();
    }
}
