namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;

    /// <summary>
    /// Implements scheduled background jobs.
    /// Registered as a scoped service and resolved inside a DI scope
    /// created by <see cref="BackgroundWorkers.BackgroundJobWorker"/>.
    /// </summary>
    public class BackgroundJobService(
        IInvoiceRepository invoiceRepository,
        IInvoiceService invoiceService,
        INotificationService notificationService) : IBackgroundJobService
    {
        public async Task<List<Guid>> CheckAndUpdateOverdueInvoicesAsync(CancellationToken ct = default)
        {
            List<Invoice> invoices = await invoiceRepository.GetOverdueEligibleAsync(ct);
            List<Guid> newlyOverdueIds = [];

            foreach (Invoice invoice in invoices)
            {
                await invoiceService.MarkAsOverdueAsync(invoice.Id);
                newlyOverdueIds.Add(invoice.Id);
            }

            return newlyOverdueIds;
        }

        public async Task GenerateAndSendDueDateRemindersAsync(CancellationToken ct = default)
        {
            await notificationService.GenerateAndSendDueDateRemindersAsync(ct);
        }
    }
}