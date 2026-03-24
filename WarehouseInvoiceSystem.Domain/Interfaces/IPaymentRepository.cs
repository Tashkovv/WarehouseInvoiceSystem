namespace WarehouseInvoiceSystem.Domain.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Domain.Queries.Results;

    public interface IPaymentRepository
    {
        Task<IEnumerable<Payment>> GetAllAsync(CancellationToken ct = default);
        Task<PagedResult<Payment>> GetPagedAsync(GetPaymentsQuery query, CancellationToken ct = default);

        Task<IEnumerable<Payment>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken ct = default);

        Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task CreateAsync(Payment payment);
        Task UpdateAsync(Payment payment);
        Task<bool> DeleteAsync(Guid id);
        Task<decimal> GetTotalPaymentsByInvoiceAsync(Guid invoiceId, CancellationToken ct = default);
        Task<IEnumerable<Payment>> GetRecentAsync(int count, CancellationToken ct = default);
    }
}