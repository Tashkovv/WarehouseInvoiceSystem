namespace WarehouseInvoiceSystem.Application.DTOs.Payment
{
    using WarehouseInvoiceSystem.Domain.Payment.Enums;

    public class UpdatePaymentDto
    {
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }
    }
}
