namespace WarehouseInvoiceSystem.Domain.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public interface IPaymentRepository
    {
        Task<IEnumerable<Payment>> GetAllAsync();
        Task<PagedResult<Payment>> GetPagedAsync(GetPaymentsQuery query);
        Task<IEnumerable<Payment>> GetByInvoiceIdAsync(Guid invoiceId);
        Task<Payment?> GetByIdAsync(Guid id);
        Task CreateAsync(Payment payment);
        Task UpdateAsync(Payment payment);
        Task<bool> DeleteAsync(Guid id);
        Task<decimal> GetTotalPaymentsByInvoiceAsync(Guid invoiceId);
    }
}