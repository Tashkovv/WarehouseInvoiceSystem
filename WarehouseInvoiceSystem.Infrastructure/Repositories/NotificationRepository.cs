namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    public class NotificationRepository(IDbContextFactory<ApplicationDbContext> factory)
        : BaseRepository(factory), INotificationRepository
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
                    .ToListAsync(ct), ct);

        public Task<bool> ExistsTodayAsync(string data, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                DateTime today = DateTime.UtcNow.Date;
                DateTime tomorrow = today.AddDays(1);
                return await All<Notification>(context)
                    .AnyAsync(n => n.Data == data && n.CreatedAt >= today && n.CreatedAt < tomorrow, ct);
            }, ct);

        public Task<Guid> CreateWithInvoicesAsync(Notification notification, List<Guid> invoiceIds, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                Insert(context, notification);
                await SaveAsync(context, ct);

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
                Notification? notification = await AllTracked<Notification>(context)
                    .FirstOrDefaultAsync(n => n.Id == notificationId, ct);

                if (notification is not null && !notification.IsRead)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                    await SaveAsync(context, ct);
                }
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
                Notification? notification = await AllTracked<Notification>(context)
                    .FirstOrDefaultAsync(n => n.Id == notificationId, ct);

                if (notification is not null)
                {
                    notification.IsEmailSent = true;
                    notification.EmailSentAt = DateTime.UtcNow;
                    await SaveAsync(context, ct);
                }
            }, ct);
    }
}
