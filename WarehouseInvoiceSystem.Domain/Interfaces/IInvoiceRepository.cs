namespace WarehouseInvoiceSystem.Domain.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Invoice.Domain;
    using WarehouseInvoiceSystem.Domain.Invoice.Enums;

    public interface IInvoiceRepository
    {
        Task<IEnumerable<Invoice>> GetAllAsync();
        Task<IEnumerable<Invoice>> GetByCompanyIdAsync(int companyId);
        Task<IEnumerable<Invoice>> GetByTypeAsync(InvoiceType type);
        Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status);
        Task<IEnumerable<Invoice>> GetOverdueInvoicesAsync();
        Task<Invoice?> GetByIdAsync(int id);
        Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber);
        Task<Invoice> CreateAsync(Invoice invoice);
        Task<Invoice> UpdateAsync(Invoice invoice);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<string> GenerateInvoiceNumberAsync(InvoiceType type);
        Task<(int total, int paid, int unpaid, int overdue)> GetInvoiceCountsAsync();
        Task<(decimal totalAmount, decimal totalPaid, decimal totalDue)> GetInvoiceTotalsAsync();
    }
}
