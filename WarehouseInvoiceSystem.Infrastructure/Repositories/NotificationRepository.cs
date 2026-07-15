namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    public class NotificationRepository(IDbContextFactory<ApplicationDbContext> factory, IAuditContextService auditContext)
        : BaseRepository(factory, auditContext), INotificationRepository
    {
        public Task<int> GetUnreadCountAsync(CancellationToken ct = default) =>
            WithContextAsync(async context =>
                await All<Notification>(context)
                    .CountAsync(n => !n.IsRead, ct), ct);

        public Task<List<Notification>> GetRecentAsync(int count, CancellationToken ct = default) =>
            WithContextAsync(async context =>
                await All<Notification>(context)
                    .Include(n => n.NotificationInvoices)
                        .ThenInclude(ni => ni.Invoice)
                            .ThenInclude(i => i.Company)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(count)
                    .AsSplitQuery()
                    .ToListAsync(ct), ct);

        public Task<bool> ExistsTodayAsync(string data, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                DateTime today = DateTime.UtcNow.Date;
                DateTime tomorrow = today.AddDays(1);
                return await All<Notification>(context)
                    .AnyAsync(n => n.Data == data && n.CreatedAt >= today && n.CreatedAt < tomorrow, ct);
            }, ct);

        public Task<List<Guid>> GetInvoiceIdsAlreadyNotifiedAsync(List<Guid> invoiceIds, NotificationType type, CancellationToken ct = default) =>
            WithContextAsync(async context =>
                await context.NotificationInvoices
                    .Where(ni => invoiceIds.Contains(ni.InvoiceId)
                              && ni.Notification.DeletedOn == null
                              && ni.Notification.Type == type)
                    .Select(ni => ni.InvoiceId)
                    .Distinct()
                    .ToListAsync(ct), ct);

        public Task<Guid> CreateWithInvoicesAsync(Notification notification, List<Guid> invoiceIds, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                Insert(context, notification);

                foreach (Guid invoiceId in invoiceIds)
                {
                    context.NotificationInvoices.Add(new NotificationInvoice
                    {
                        NotificationId = notification.Id,
                        InvoiceId = invoiceId
                    });
                }

                await SaveAsync(context, ct);
                return notification.Id;
            }, ct);

        public Task MarkAsReadAsync(Guid notificationId, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                await context.Notifications
                    .Where(n => n.DeletedOn == null && n.Id == notificationId && !n.IsRead)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(n => n.IsRead, true)
                        .SetProperty(n => n.ReadAt, DateTime.UtcNow), ct);
            }, ct);

        public Task MarkAllAsReadAsync(CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                await context.Notifications
                    .Where(n => n.DeletedOn == null && !n.IsRead)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(n => n.IsRead, true)
                        .SetProperty(n => n.ReadAt, DateTime.UtcNow), ct);
            }, ct);

        public Task MarkEmailSentAsync(Guid notificationId, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                await context.Notifications
                    .Where(n => n.DeletedOn == null && n.Id == notificationId && !n.IsEmailSent)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(n => n.IsEmailSent, true)
                        .SetProperty(n => n.EmailSentAt, DateTime.UtcNow), ct);
            }, ct);
    }
}
