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

        private async Task GenerateReceivableRemindersAsync(int[] thresholds, CancellationToken ct)
        {
            foreach (int days in thresholds)
            {
                if (ct.IsCancellationRequested) return;

                List<Invoice> invoices = await invoiceRepository.GetInvoicesDueInDaysAsync(days, InvoiceType.Receivable, ct);
                if (invoices.Count == 0) continue;

                string data = JsonSerializer.Serialize(new { daysBeforeDue = days, invoiceType = "Receivable" });
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

                // Send email to each client company (grouped by CompanyId)
                bool anySent = false;
                var invoicesByCompany = invoices.GroupBy(i => i.CompanyId);

                foreach (var group in invoicesByCompany)
                {
                    Invoice first = group.First();
                    string? companyEmail = first.Company?.Email;
                    string companyName = first.Company?.Name ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(companyEmail)) continue;

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

        private async Task GeneratePayableRemindersAsync(int[] thresholds, CancellationToken ct)
        {
            foreach (int days in thresholds)
            {
                if (ct.IsCancellationRequested) return;

                List<Invoice> invoices = await invoiceRepository.GetInvoicesDueInDaysAsync(days, InvoiceType.Payable, ct);
                if (invoices.Count == 0) continue;

                string data = JsonSerializer.Serialize(new { daysBeforeDue = days, invoiceType = "Payable" });
                if (await notificationRepository.ExistsTodayAsync(data, ct)) continue;
                var notification = new Notification
                {
                    Type = NotificationType.InvoiceDueReminder,
                    Data = data,
                    IsRead = false,
                    IsEmailSent = false
                };

                List<Guid> invoiceIds = invoices.Select(i => i.Id).ToList();
                await notificationRepository.CreateWithInvoicesAsync(notification, invoiceIds, ct);
                // No email for payable invoices
            }
        }
    }
}
