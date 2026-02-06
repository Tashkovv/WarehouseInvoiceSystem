namespace WarehouseInvoiceSystem.Application.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendInvoiceEmailAsync(int invoiceId, string? customMessage = null);
        Task<bool> SendTestEmailAsync(string recipientEmail);
    }
}
