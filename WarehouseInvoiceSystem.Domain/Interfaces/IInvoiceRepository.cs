namespace WarehouseInvoiceSystem.Domain.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public interface IInvoiceRepository
    {
        Task<IEnumerable<Invoice>> GetAllAsync();
        Task<PagedResult<Invoice>> GetPagedAsync(GetInvoicesQuery query);
        Task<IEnumerable<Invoice>> GetByCompanyIdAsync(Guid companyId);
        Task<IEnumerable<Invoice>> GetByTypeAsync(InvoiceType type);
        Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status);
        Task<IEnumerable<Invoice>> GetOverdueInvoicesAsync();
        Task<Invoice?> GetByIdAsync(Guid id);
        Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber);
        Task<IEnumerable<InvoiceLine>> GetLineItemsByProductIdAsync(Guid productId);
        Task<Invoice> CreateAsync(Invoice invoice);
        Task<Invoice> UpdateAsync(Invoice invoice);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<string> GenerateInvoiceNumberAsync(InvoiceType type);
        Task<(int total, int paid, int unpaid, int overdue)> GetPayableInvoiceCountsAsync();
        Task<(decimal totalAmount, decimal totalPaid, decimal totalDue)> GetPayableInvoiceTotalsAsync();
    }
}
