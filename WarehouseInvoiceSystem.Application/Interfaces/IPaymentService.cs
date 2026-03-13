namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.Payment;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public interface IPaymentService
    {
        Task<IEnumerable<PaymentDto>> GetAllPaymentsAsync();
        Task<PagedResult<PaymentDto>> GetPagedAsync(GetPaymentsQuery query);
        Task<IEnumerable<PaymentDto>> GetPaymentsByInvoiceAsync(Guid invoiceId);
        Task<PaymentDto?> GetPaymentByIdAsync(Guid id);
        Task CreatePaymentAsync(CreatePaymentDto createDto);
        Task UpdatePaymentAsync(Guid id, UpdatePaymentDto updateDto);
        Task<bool> DeletePaymentAsync(Guid id);
    }
}