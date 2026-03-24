namespace WarehouseInvoiceSystem.Application.Interfaces
{
    public interface IBackgroundJobService
    {
        /// <summary>
        /// Checks invoices and marks them as Overdue if due date has passed
        /// </summary>
        Task<List<Guid>> CheckAndUpdateOverdueInvoicesAsync();

        /// <summary>
        /// Generates due-date reminder notifications for receivable and payable invoices.
        /// Sends email reminders to client companies for receivable invoices.
        /// </summary>
        Task GenerateAndSendDueDateRemindersAsync(CancellationToken ct = default);
    }
}