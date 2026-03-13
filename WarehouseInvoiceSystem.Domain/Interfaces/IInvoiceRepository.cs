namespace WarehouseInvoiceSystem.Domain.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public interface IInvoiceRepository
    {
        Task<IEnumerable<Invoice>> GetAllAsync(CancellationToken ct = default);
        Task<PagedResult<Invoice>> GetPagedAsync(GetInvoicesQuery query, CancellationToken ct = default);

        Task<IEnumerable<Invoice>> GetByCompanyIdAsync(Guid companyId, CancellationToken ct = default);

        Task<IEnumerable<Invoice>> GetByTypeAsync(InvoiceType type, CancellationToken ct = default);

        Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status, CancellationToken ct = default);

        Task<IEnumerable<Invoice>> GetOverdueInvoicesAsync(CancellationToken ct = default);
        Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken ct = default);

        Task<IEnumerable<InvoiceLine>> GetLineItemsByProductIdAsync(Guid productId, CancellationToken ct = default);

        Task<PagedResult<InvoiceLine>> GetPagedLineItemsByProductIdAsync(GetProductHistoryQuery query, CancellationToken ct = default);

        Task<Guid> CreateAsync(Invoice invoice);
        Task<Invoice> UpdateAsync(Invoice invoice);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

        Task<string> GenerateInvoiceNumberAsync(InvoiceType type, CancellationToken ct = default);

        Task<(int total, int paid, int unpaid, int overdue)> GetPayableInvoiceCountsAsync(CancellationToken ct = default);
        Task<(decimal totalAmount, decimal totalPaid, decimal totalDue)> GetPayableInvoiceTotalsAsync(CancellationToken ct = default);
    }
}