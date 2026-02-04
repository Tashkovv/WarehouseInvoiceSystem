namespace WarehouseInvoiceSystem.Application.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendInvoiceEmailAsync(int invoiceId, string recipientEmail, string? customMessage = null);
        Task<bool> SendTestEmailAsync(string recipientEmail);
    }
}
