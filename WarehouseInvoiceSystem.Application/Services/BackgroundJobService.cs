namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
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
        public async Task<List<Guid>> CheckAndUpdateOverdueInvoicesAsync()
        {
            IEnumerable<Invoice> invoices = await invoiceRepository.GetAllAsync();

            DateTime today = DateTime.UtcNow.Date;
            List<Guid> newlyOverdueIds = [];

            foreach (Invoice invoice in invoices)
            {
                if (invoice.DueDate < today &&
                    invoice.Status != InvoiceStatus.Draft &&
                    invoice.Status != InvoiceStatus.Paid &&
                    invoice.Status != InvoiceStatus.Cancelled &&
                    invoice.Status != InvoiceStatus.Overdue)
                {
                    await invoiceService.MarkAsOverdueAsync(invoice.Id);
                    newlyOverdueIds.Add(invoice.Id);
                }
            }

            return newlyOverdueIds;
        }

        public async Task GenerateAndSendDueDateRemindersAsync(CancellationToken ct = default)
        {
            await notificationService.GenerateAndSendDueDateRemindersAsync(ct);
        }
    }
}