namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.Payment;

    public interface IPaymentService
    {
        Task<IEnumerable<PaymentDto>> GetAllPaymentsAsync();
        Task<IEnumerable<PaymentDto>> GetPaymentsByInvoiceAsync(int invoiceId);
        Task<PaymentDto?> GetPaymentByIdAsync(int id);
        Task<PaymentDto> CreatePaymentAsync(CreatePaymentDto createDto);
        Task<PaymentDto> UpdatePaymentAsync(int id, UpdatePaymentDto updateDto);
        Task<bool> DeletePaymentAsync(int id);
    }
}
