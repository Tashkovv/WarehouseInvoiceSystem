namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Entities;

    public interface IEmailService
    {
        Task<bool> SendInvoiceEmailAsync(Guid invoiceId, string? customMessage = null);
        Task<bool> SendTestEmailAsync(string recipientEmail);
        Task<bool> SendDueDateReminderEmailAsync(int daysBeforeDue, string companyName, string companyEmail, List<Invoice> invoices, CancellationToken ct = default);
    }
}
