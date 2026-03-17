namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.Payment;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public interface IPaymentService
    {
        Task<IEnumerable<PaymentDto>> GetAllPaymentsAsync(CancellationToken ct = default);
        Task<PagedResult<PaymentDto>> GetPagedAsync(GetPaymentsQuery query, CancellationToken ct = default);
        Task<IEnumerable<PaymentDto>> GetPaymentsByInvoiceAsync(Guid invoiceId, CancellationToken ct = default);
        Task<PaymentDto?> GetPaymentByIdAsync(Guid id, CancellationToken ct = default);
        Task CreatePaymentAsync(CreatePaymentDto createDto);
        Task UpdatePaymentAsync(Guid id, UpdatePaymentDto updateDto);
        Task UpdateNotesAsync(Guid id, string? notes, CancellationToken ct = default);
        Task<bool> DeletePaymentAsync(Guid id);
        Task<IEnumerable<PaymentDto>> GetRecentAsync(int count, CancellationToken ct = default);
    }
}