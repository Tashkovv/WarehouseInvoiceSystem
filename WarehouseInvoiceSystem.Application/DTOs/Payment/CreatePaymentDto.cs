namespace WarehouseInvoiceSystem.Application.DTOs.Payment
{
    using WarehouseInvoiceSystem.Domain.Enums;

    public class CreatePaymentDto
    {
        public int InvoiceId { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }
        public string? RecordedBy { get; set; }
    }
}
