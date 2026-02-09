namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.Payment;

    public interface IPaymentService
    {
        Task<IEnumerable<PaymentDto>> GetAllPaymentsAsync();
        Task<IEnumerable<PaymentDto>> GetPaymentsByInvoiceAsync(Guid invoiceId);
        Task<PaymentDto?> GetPaymentByIdAsync(Guid id);
        Task<PaymentDto> CreatePaymentAsync(CreatePaymentDto createDto);
        Task<PaymentDto> UpdatePaymentAsync(Guid id, UpdatePaymentDto updateDto);
        Task<bool> DeletePaymentAsync(Guid id);
    }
}
