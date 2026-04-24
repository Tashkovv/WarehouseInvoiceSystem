namespace WarehouseInvoiceSystem.Application.Services
{
    using System.Text.Json;
    using Microsoft.Extensions.Options;
    using WarehouseInvoiceSystem.Application.DTOs.Notification;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Application.Settings;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Interfaces;

    public class NotificationService(
        INotificationRepository notificationRepository,
        IInvoiceRepository invoiceRepository,
        IEmailService emailService,
        IOptions<NotificationSettings> notificationSettings) : INotificationService
    {
        private readonly NotificationSettings _settings = notificationSettings.Value;

        public async Task<int> GetUnreadCountAsync(CancellationToken ct = default) =>
            await notificationRepository.GetUnreadCountAsync(ct);

        public async Task<List<NotificationDto>> GetRecentNotificationsAsync(int count = 20, CancellationToken ct = default)
        {
            List<Notification> notifications = await notificationRepository.GetRecentAsync(count, ct);

            return notifications.Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Data = n.Data,
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                IsEmailSent = n.IsEmailSent,
                CreatedAt = n.CreatedAt,
                Invoices = n.NotificationInvoices.Select(ni => new NotificationInvoiceDto
                {
                    InvoiceId = ni.InvoiceId,
                    InvoiceNumber = ni.Invoice.InvoiceNumber,
                    CompanyName = ni.Invoice.Company.Name,
                    TotalAmount = ni.Invoice.TotalAmount,
                    AmountDue = ni.Invoice.AmountDue,
                    DueDate = ni.Invoice.DueDate
                }).ToList()
            }).ToList();
        }

        public async Task MarkAsReadAsync(Guid id, CancellationToken ct = default) =>
            await notificationRepository.MarkAsReadAsync(id, ct);

        public async Task MarkAllAsReadAsync(CancellationToken ct = default) =>
            await notificationRepository.MarkAllAsReadAsync(ct);

        public async Task GenerateAndSendDueDateRemindersAsync(CancellationToken ct = default)
        {
            if (_settings.ReceivableDays is { Length: > 0 })
                await GenerateReceivableRemindersAsync(_settings.ReceivableDays, ct);

            if (_settings.PayableDays is { Length: > 0 })
                await GeneratePayableRemindersAsync(_settings.PayableDays, ct);
        }

        private Task GenerateReceivableRemindersAsync(int[] thresholds, CancellationToken ct)
            => GenerateRemindersAsync(thresholds, InvoiceType.Receivable, sendEmail: true, ct);

        private Task GeneratePayableRemindersAsync(int[] thresholds, CancellationToken ct)
            => GenerateRemindersAsync(thresholds, InvoiceType.Payable, sendEmail: false, ct);

        private async Task GenerateRemindersAsync(int[] thresholds, InvoiceType type, bool sendEmail, CancellationToken ct)
        {
            string typeName = type.ToString();

            foreach (int days in thresholds)
            {
                if (ct.IsCancellationRequested) return;

                List<Invoice> invoices = await invoiceRepository.GetInvoicesDueInDaysAsync(days, type, ct);
                if (invoices.Count == 0) continue;

                string data = JsonSerializer.Serialize(new { daysBeforeDue = days, invoiceType = typeName });
                if (await notificationRepository.ExistsTodayAsync(data, ct)) continue;

                var notification = new Notification
                {
                    Type = NotificationType.InvoiceDueReminder,
                    Data = data,
                    IsRead = false,
                    IsEmailSent = false
                };

                List<Guid> invoiceIds = invoices.Select(i => i.Id).ToList();
                Guid notificationId = await notificationRepository.CreateWithInvoicesAsync(notification, invoiceIds, ct);

                if (sendEmail && _settings.SendEmails)
                    await SendReminderEmailsAsync(days, invoices, notificationId, ct);
            }
        }

        public async Task CreateOverdueNotificationAsync(List<Guid> invoiceIds, CancellationToken ct = default)
        {
            string data = JsonSerializer.Serialize(new { count = invoiceIds.Count });
            if (await notificationRepository.ExistsTodayAsync(data, ct)) return;

            var notification = new Notification
            {
                Type = NotificationType.InvoiceOverdue,
                Data = data,
                IsRead = false,
                IsEmailSent = false
            };

            Guid notificationId = await notificationRepository.CreateWithInvoicesAsync(notification, invoiceIds, ct);

            if (_settings.SendEmails)
            {
                List<Invoice> invoices = await invoiceRepository.GetByIdsWithCompanyAsync(invoiceIds, ct);
                await SendOverdueEmailsAsync(invoices, notificationId, ct);
            }
        }

        public async Task CreateLicenseExpiringNotificationAsync(int graceDaysRemaining, CancellationToken ct = default)
        {
            string data = JsonSerializer.Serialize(new { graceDaysRemaining });
            if (await notificationRepository.ExistsTodayAsync(data, ct)) return;

            var notification = new Notification
            {
                Type = NotificationType.LicenseExpiring,
                Data = data,
                IsRead = false,
                IsEmailSent = false
            };

            await notificationRepository.CreateWithInvoicesAsync(notification, [], ct);
        }

        private async Task SendOverdueEmailsAsync(List<Invoice> invoices, Guid notificationId, CancellationToken ct)
        {
            bool anySent = false;

            foreach (var group in invoices.GroupBy(i => i.CompanyId))
            {
                Invoice first = group.First();
                string? companyEmail = first.Company?.Email;
                if (string.IsNullOrWhiteSpace(companyEmail)) continue;

                string companyName = first.Company?.Name ?? string.Empty;

                try
                {
                    bool sent = await emailService.SendOverdueNotificationEmailAsync(
                        companyName, companyEmail, group.ToList(), ct);
                    if (sent) anySent = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending overdue notification to {companyName}: {ex.Message}");
                }
            }

            if (anySent)
            {
                try { await notificationRepository.MarkEmailSentAsync(notificationId, ct); }
                catch { /* best-effort */ }
            }
        }

        private async Task SendReminderEmailsAsync(int days, List<Invoice> invoices, Guid notificationId, CancellationToken ct)
        {
            bool anySent = false;

            foreach (var group in invoices.GroupBy(i => i.CompanyId))
            {
                Invoice first = group.First();
                string? companyEmail = first.Company?.Email;
                if (string.IsNullOrWhiteSpace(companyEmail)) continue;

                string companyName = first.Company?.Name ?? string.Empty;

                try
                {
                    bool sent = await emailService.SendDueDateReminderEmailAsync(
                        days, companyName, companyEmail, group.ToList(), ct);
                    if (sent) anySent = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending reminder to {companyName}: {ex.Message}");
                }
            }

            if (anySent)
            {
                try { await notificationRepository.MarkEmailSentAsync(notificationId, ct); }
                catch { /* best-effort */ }
            }
        }
    }
}
