namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Interfaces;

    /// <summary>
    /// Implements scheduled background jobs.
    /// Registered as a scoped service and resolved inside a DI scope
    /// created by <see cref="BackgroundWorkers.BackgroundJobWorker"/>.
    /// </summary>
    public class BackgroundJobService(
        IInvoiceRepository invoiceRepository,
        INotificationService notificationService) : IBackgroundJobService
    {
        public Task<List<Guid>> CheckAndUpdateOverdueInvoicesAsync(CancellationToken ct = default)
            => invoiceRepository.BulkMarkOverdueAsync(ct);

        public async Task GenerateAndSendDueDateRemindersAsync(CancellationToken ct = default)
        {
            await notificationService.GenerateAndSendDueDateRemindersAsync(ct);
        }
    }
}