namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.Notification;

    public interface INotificationService
    {
        Task<int> GetUnreadCountAsync(CancellationToken ct = default);
        Task<List<NotificationDto>> GetRecentNotificationsAsync(int count = 20, CancellationToken ct = default);
        Task MarkAsReadAsync(Guid id, CancellationToken ct = default);
        Task MarkAllAsReadAsync(CancellationToken ct = default);
        Task GenerateAndSendDueDateRemindersAsync(CancellationToken ct = default);
    }
}
