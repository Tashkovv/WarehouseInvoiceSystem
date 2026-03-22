namespace WarehouseInvoiceSystem.Domain.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Entities;

    public interface INotificationRepository
    {
        Task<int> GetUnreadCountAsync(CancellationToken ct = default);
        Task<List<Notification>> GetRecentAsync(int count, CancellationToken ct = default);
        Task<bool> ExistsTodayAsync(string data, CancellationToken ct = default);
        Task<Guid> CreateWithInvoicesAsync(Notification notification, List<Guid> invoiceIds, CancellationToken ct = default);
        Task MarkAsReadAsync(Guid notificationId, CancellationToken ct = default);
        Task MarkAllAsReadAsync(CancellationToken ct = default);
        Task MarkEmailSentAsync(Guid notificationId, CancellationToken ct = default);
    }
}
