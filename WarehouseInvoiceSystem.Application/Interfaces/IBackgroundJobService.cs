namespace WarehouseInvoiceSystem.Application.Interfaces
{
    public interface IBackgroundJobService
    {
        /// <summary>
        /// Checks invoices and marks them as Overdue if due date has passed
        /// </summary>
        Task CheckAndUpdateOverdueInvoicesAsync();

        // Future background jobs can be added here:
        // Task SendOverdueInvoiceRemindersAsync();
        // Task GenerateMonthlyReportsAsync();
        // Task CleanupOldLogsAsync();
        // etc.
    }
}
