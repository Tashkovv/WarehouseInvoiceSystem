namespace WarehouseInvoiceSystem.Application.Services
{
    using MailKit.Net.Smtp;
    using MailKit.Security;
    using Microsoft.Extensions.Options;
    using MimeKit;
    using WarehouseInvoiceSystem.Application.DTOs.Invoice;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Application.Settings;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Interfaces;

    public class EmailService(
        IOptions<EmailSettings> emailSettings,
        ITenantRepository tenantRepository,
        IEncryptionService encryptionService,
        IInvoiceService invoiceService,
        IExcelExportService excelExportService,
        ILocalizationService translations) : IEmailService
    {
        private readonly EmailSettings _emailSettings = emailSettings.Value;

        public async Task SendInvoiceEmailAsync(Guid invoiceId, string? customMessage = null)
        {
            InvoiceDto invoice = await invoiceService.GetInvoiceByIdAsync(invoiceId)
                ?? throw new KeyNotFoundException($"Invoice with ID {invoiceId} not found");

            if (string.IsNullOrWhiteSpace(invoice.CompanyEmail))
                throw new InvalidOperationException(
                    $"Invoice {invoice.InvoiceNumber} has no recipient email address.");

            Tenant tenant = await tenantRepository.GetAsync();
            (string username, string password) = ResolveCredentials(tenant);

            MimeMessage message = new();
            message.From.Add(new MailboxAddress(tenant.CompanyName, username));
            message.To.Add(new MailboxAddress(invoice.CompanyName, invoice.CompanyEmail));
            message.Subject = $"{translations.GetString("Invoice")} {invoice.InvoiceNumber} {translations.GetString("From")} {tenant.CompanyName}";

            BodyBuilder bodyBuilder = new()
            {
                HtmlBody = translations.CurrentLanguage.Equals("mk")
                           ? BuildMkdInvoiceEmailHtml(invoice, customMessage, tenant)
                           : BuildEnglishInvoiceEmailHtml(invoice, customMessage, tenant)
            };

            byte[] invoiceExcel = await excelExportService.ExportInvoiceForPrintingAsync(invoice);
            bodyBuilder.Attachments.Add($"{translations.GetString("Invoice")}_{invoice.InvoiceNumber}.xlsx", invoiceExcel);

            message.Body = bodyBuilder.ToMessageBody();

            await SendAsync(message, username, password);
        }

        public async Task<bool> SendTestEmailAsync(string recipientEmail)
        {
            try
            {
                Tenant tenant = await tenantRepository.GetAsync();
                (string username, string password) = ResolveCredentials(tenant);

                MimeMessage message = new();
                message.From.Add(new MailboxAddress(tenant.CompanyName, username));
                message.To.Add(new MailboxAddress("Test Recipient", recipientEmail));
                message.Subject = "Test Email - Invoice System";

                BodyBuilder bodyBuilder = new()
                {
                    HtmlBody = @"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <h2>Test Email Successful!</h2>
                        <p>Your email settings are configured correctly.</p>
                        <p>This is a test email from the Invoice System.</p>
                        <br/>
                        <p style='color: #666; font-size: 12px;'>
                            If you received this email, your email configuration is working properly.
                        </p>
                    </body>
                    </html>"
                };

                message.Body = bodyBuilder.ToMessageBody();

                await SendAsync(message, username, password);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending test email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendDueDateReminderEmailAsync(int daysBeforeDue, string companyName,
            string companyEmail, List<Invoice> invoices, CancellationToken ct = default)
        {
            try
            {
                Tenant tenant = await tenantRepository.GetAsync(ct);
                (string username, string password) = ResolveCredentials(tenant);

                MimeMessage message = new();
                message.From.Add(new MailboxAddress(tenant.CompanyName, username));
                message.To.Add(new MailboxAddress(companyName, companyEmail));

                string subjectKey = daysBeforeDue == 1 ? "DueDateReminderSubjectTomorrow" : "DueDateReminderSubjectDays";
                message.Subject = daysBeforeDue == 1
                    ? string.Format(translations.GetString(subjectKey), invoices.Count)
                    : string.Format(translations.GetString(subjectKey), invoices.Count, daysBeforeDue);

                bool isMk = translations.CurrentLanguage.Equals("mk");

                BodyBuilder bodyBuilder = new()
                {
                    HtmlBody = isMk
                        ? BuildMkdReminderHtml(daysBeforeDue, companyName, invoices, tenant)
                        : BuildEnglishReminderHtml(daysBeforeDue, companyName, invoices, tenant)
                };

                message.Body = bodyBuilder.ToMessageBody();
                await SendAsync(message, username, password);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending reminder email to {companyName}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendOverdueNotificationEmailAsync(string companyName, string companyEmail,
            List<Invoice> invoices, CancellationToken ct = default)
        {
            try
            {
                Tenant tenant = await tenantRepository.GetAsync(ct);
                (string username, string password) = ResolveCredentials(tenant);

                MimeMessage message = new();
                message.From.Add(new MailboxAddress(tenant.CompanyName, username));
                message.To.Add(new MailboxAddress(companyName, companyEmail));

                message.Subject = string.Format(translations.GetString("OverdueEmailSubject"), invoices.Count);

                bool isMk = translations.CurrentLanguage.Equals("mk");

                BodyBuilder bodyBuilder = new()
                {
                    HtmlBody = isMk
                        ? BuildMkdOverdueHtml(companyName, invoices, tenant)
                        : BuildEnglishOverdueHtml(companyName, invoices, tenant)
                };

                message.Body = bodyBuilder.ToMessageBody();
                await SendAsync(message, username, password);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending overdue notification to {companyName}: {ex.Message}");
                return false;
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Resolves SMTP credentials from the Tenant entity.
        /// Throws <see cref="InvalidOperationException"/> with a clear, actionable
        /// message if either the email address or the encrypted password is absent.
        /// There is intentionally no appsettings fallback.
        /// </summary>
        private (string username, string password) ResolveCredentials(Tenant tenant)
        {
            if (string.IsNullOrEmpty(tenant.Email) || string.IsNullOrEmpty(tenant.EmailPasswordEncrypted))
                throw new InvalidOperationException(
                    "Email credentials are not configured. Please update your Profile settings.");

            return (tenant.Email, encryptionService.Decrypt(tenant.EmailPasswordEncrypted));
        }

        private async Task SendAsync(MimeMessage message, string username, string password)
        {
            using SmtpClient client = new();
            await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        private static string BuildMkdInvoiceEmailHtml(InvoiceDto invoice, string? customMessage, Tenant tenant)
        {
            string invoiceType = invoice.Type == InvoiceType.Receivable ? "Фактура" : "Сметка";
            string greeting = invoice.Type == InvoiceType.Receivable
                ? $"Почитувани {invoice.CompanyName},"
                : "Почитувани,";
            string customSection = !string.IsNullOrWhiteSpace(customMessage) ? $"<p>{customMessage}</p>" : "";
            string footer = BuildFooterLines(tenant);

            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #1E40AF; color: white; padding: 20px; text-align: center; }}
                    .content {{ padding: 20px; background-color: #f9f9f9; }}
                    .invoice-details {{ background-color: white; padding: 15px; margin: 20px 0; border-left: 4px solid #1E40AF; }}
                    .detail-row {{ margin: 10px 0; }}
                    .label {{ font-weight: bold; color: #666; }}
                    .value {{ color: #333; }}
                    .amount {{ font-size: 24px; font-weight: bold; color: #1E40AF; }}
                    .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>{tenant.CompanyName}</h1>
                        <p>Систем за фактури</p>
                    </div>
                    <div class='content'>
                        <h2>{invoiceType} {invoice.InvoiceNumber}</h2>
                        <p>{greeting}</p>
                        {customSection}
                        <p>Ве молиме пронајдете ја приложената {invoiceType.ToLower()} за ваша референца.</p>
                        <div class='invoice-details'>
                            <div class='detail-row'>
                                <span class='label'>{invoiceType} број:</span>
                                <span class='value'>{invoice.InvoiceNumber}</span>
                            </div>
                            <div class='detail-row'>
                                <span class='label'>Датум на издавање:</span>
                                <span class='value'>{invoice.IssueDate:MMMM dd, yyyy}</span>
                            </div>
                            <div class='detail-row'>
                                <span class='label'>Рок на плаќање:</span>
                                <span class='value'>{invoice.DueDate:MMMM dd, yyyy}</span>
                            </div>
                            <div class='detail-row' style='margin-top: 20px;'>
                                <span class='label'>Вкупна сума:</span>
                                <span class='amount'>{invoice.TotalAmount:C}</span>
                            </div>
                        </div>
                        <p>Доколку имате какви било прашања во врска со оваа {invoiceType.ToLower()}, слободно контактирајте не.</p>
                        <p>Ви благодариме за соработката!</p>
                    </div>
                    <div class='footer'>
                        <p><strong>{tenant.CompanyName}</strong></p>
                        {footer}
                        <p>Ова е автоматска порака, ве молиме не одговарајте на оваа е-пошта.</p>
                    </div>
                </div>
            </body>
            </html>";
        }

        private static string BuildEnglishInvoiceEmailHtml(InvoiceDto invoice, string? customMessage, Tenant tenant)
        {
            string invoiceType = invoice.Type == InvoiceType.Receivable ? "Invoice" : "Bill";
            string greeting = invoice.Type == InvoiceType.Receivable
                ? $"Dear {invoice.CompanyName},"
                : "Dear Team,";
            string customSection = !string.IsNullOrWhiteSpace(customMessage) ? $"<p>{customMessage}</p>" : "";
            string footer = BuildFooterLines(tenant);

            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #1E40AF; color: white; padding: 20px; text-align: center; }}
                    .content {{ padding: 20px; background-color: #f9f9f9; }}
                    .invoice-details {{ background-color: white; padding: 15px; margin: 20px 0; border-left: 4px solid #1E40AF; }}
                    .detail-row {{ margin: 10px 0; }}
                    .label {{ font-weight: bold; color: #666; }}
                    .value {{ color: #333; }}
                    .amount {{ font-size: 24px; font-weight: bold; color: #1E40AF; }}
                    .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>{tenant.CompanyName}</h1>
                        <p>Invoice System</p>
                    </div>
                    <div class='content'>
                        <h2>{invoiceType} {invoice.InvoiceNumber}</h2>
                        <p>{greeting}</p>
                        {customSection}
                        <p>Please find the attached {invoiceType.ToLower()} for your reference.</p>
                        <div class='invoice-details'>
                            <div class='detail-row'>
                                <span class='label'>{invoiceType} Number:</span>
                                <span class='value'>{invoice.InvoiceNumber}</span>
                            </div>
                            <div class='detail-row'>
                                <span class='label'>Issue Date:</span>
                                <span class='value'>{invoice.IssueDate:MMMM dd, yyyy}</span>
                            </div>
                            <div class='detail-row'>
                                <span class='label'>Due Date:</span>
                                <span class='value'>{invoice.DueDate:MMMM dd, yyyy}</span>
                            </div>
                            <div class='detail-row' style='margin-top: 20px;'>
                                <span class='label'>Total Amount:</span>
                                <span class='amount'>{invoice.TotalAmount:C}</span>
                            </div>
                        </div>
                        <p>If you have any questions about this {invoiceType.ToLower()}, please don't hesitate to contact us.</p>
                        <p>Thank you for your business!</p>
                    </div>
                    <div class='footer'>
                        <p><strong>{tenant.CompanyName}</strong></p>
                        {footer}
                        <p>This is an automated message, please do not reply to this email.</p>
                    </div>
                </div>
            </body>
            </html>";
        }

        /// <summary>
        /// Builds footer contact lines from whatever tenant fields are populated.
        /// Only non-empty fields are included so the footer stays clean when the
        /// Profile is partially filled in.
        /// </summary>
        private static string BuildFooterLines(Tenant tenant)
        {
            List<string> lines = [];
            if (!string.IsNullOrWhiteSpace(tenant.Address)) lines.Add(tenant.Address);
            if (!string.IsNullOrWhiteSpace(tenant.Phone)) lines.Add(tenant.Phone);
            if (!string.IsNullOrWhiteSpace(tenant.Website)) lines.Add(tenant.Website);
            return string.Join("", lines.Select(l => $"<p>{l}</p>"));
        }

        private static string BuildEnglishReminderHtml(int daysBeforeDue, string companyName, List<Invoice> invoices, Tenant tenant)
        {
            string footer = BuildFooterLines(tenant);
            string dueText = daysBeforeDue == 1 ? "due tomorrow" : $"due in {daysBeforeDue} days";
            string rows = string.Join("", invoices.Select(i => $@"
                <tr>
                    <td style='padding: 8px 12px; border-bottom: 1px solid #eee;'>{i.InvoiceNumber}</td>
                    <td style='padding: 8px 12px; border-bottom: 1px solid #eee; text-align: right;'>{i.DueDate:MMMM dd, yyyy}</td>
                    <td style='padding: 8px 12px; border-bottom: 1px solid #eee; text-align: right;'>{i.AmountDue:C}</td>
                </tr>"));

            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #1E40AF; color: white; padding: 20px; text-align: center; }}
                    .content {{ padding: 20px; background-color: #f9f9f9; }}
                    .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
                    table {{ width: 100%; border-collapse: collapse; background: white; }}
                    th {{ padding: 10px 12px; background-color: #1E40AF; color: white; text-align: left; }}
                    th:not(:first-child) {{ text-align: right; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>{tenant.CompanyName}</h1>
                        <p>Payment Reminder</p>
                    </div>
                    <div class='content'>
                        <p>Dear {companyName},</p>
                        <p>This is a friendly reminder that the following {invoices.Count} invoice(s) are {dueText}:</p>
                        <table>
                            <thead>
                                <tr>
                                    <th>Invoice</th>
                                    <th style='text-align: right;'>Due Date</th>
                                    <th style='text-align: right;'>Amount Due</th>
                                </tr>
                            </thead>
                            <tbody>{rows}</tbody>
                        </table>
                        <p style='margin-top: 20px;'>Please ensure timely payment to avoid any late fees.</p>
                        <p>If you have already made the payment, please disregard this reminder.</p>
                        <p>Thank you for your business!</p>
                    </div>
                    <div class='footer'>
                        <p><strong>{tenant.CompanyName}</strong></p>
                        {footer}
                        <p>This is an automated message, please do not reply to this email.</p>
                    </div>
                </div>
            </body>
            </html>";
        }

        private static string BuildMkdReminderHtml(int daysBeforeDue, string companyName, List<Invoice> invoices, Tenant tenant)
        {
            string footer = BuildFooterLines(tenant);
            string dueIntro = daysBeforeDue == 1
                ? $"Утре е рокот за плаќање на {invoices.Count} фактура/и:"
                : $"Рокот за плаќање на {invoices.Count} фактура/и истекува за {daysBeforeDue} дена:";
            string rows = string.Join("", invoices.Select(i => $@"
                <tr>
                    <td style='padding: 8px 12px; border-bottom: 1px solid #eee;'>{i.InvoiceNumber}</td>
                    <td style='padding: 8px 12px; border-bottom: 1px solid #eee; text-align: right;'>{i.DueDate:MMMM dd, yyyy}</td>
                    <td style='padding: 8px 12px; border-bottom: 1px solid #eee; text-align: right;'>{i.AmountDue:C}</td>
                </tr>"));

            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #1E40AF; color: white; padding: 20px; text-align: center; }}
                    .content {{ padding: 20px; background-color: #f9f9f9; }}
                    .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
                    table {{ width: 100%; border-collapse: collapse; background: white; }}
                    th {{ padding: 10px 12px; background-color: #1E40AF; color: white; text-align: left; }}
                    th:not(:first-child) {{ text-align: right; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>{tenant.CompanyName}</h1>
                        <p>Потсетник за плаќање</p>
                    </div>
                    <div class='content'>
                        <p>Почитувани {companyName},</p>
                        <p>{dueIntro}</p>
                        <table>
                            <thead>
                                <tr>
                                    <th>Фактура</th>
                                    <th style='text-align: right;'>Рок</th>
                                    <th style='text-align: right;'>Износ</th>
                                </tr>
                            </thead>
                            <tbody>{rows}</tbody>
                        </table>
                        <p style='margin-top: 20px;'>Ве молиме обезбедете навремено плаќање.</p>
                        <p>Доколку веќе сте ја извршиле уплатата, занемарете го овој потсетник.</p>
                        <p>Ви благодариме за соработката!</p>
                    </div>
                    <div class='footer'>
                        <p><strong>{tenant.CompanyName}</strong></p>
                        {footer}
                        <p>Ова е автоматска порака, ве молиме не одговарајте на оваа е-пошта.</p>
                    </div>
                </div>
            </body>
            </html>";
        }

        private static string BuildEnglishOverdueHtml(string companyName, List<Invoice> invoices, Tenant tenant)
        {
            string footer = BuildFooterLines(tenant);
            string rows = string.Join("", invoices.Select(i => $@"
                <tr>
                    <td style='padding: 8px 12px; border-bottom: 1px solid #eee;'>{i.InvoiceNumber}</td>
                    <td style='padding: 8px 12px; border-bottom: 1px solid #eee; text-align: right;'>{i.DueDate:MMMM dd, yyyy}</td>
                    <td style='padding: 8px 12px; border-bottom: 1px solid #eee; text-align: right;'>{i.AmountDue:C}</td>
                </tr>"));

            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #B91C1C; color: white; padding: 20px; text-align: center; }}
                    .content {{ padding: 20px; background-color: #f9f9f9; }}
                    .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
                    table {{ width: 100%; border-collapse: collapse; background: white; }}
                    th {{ padding: 10px 12px; background-color: #B91C1C; color: white; text-align: left; }}
                    th:not(:first-child) {{ text-align: right; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>{tenant.CompanyName}</h1>
                        <p>Overdue Invoice Notice</p>
                    </div>
                    <div class='content'>
                        <p>Dear {companyName},</p>
                        <p>The following {invoices.Count} invoice(s) are now <strong>overdue</strong>:</p>
                        <table>
                            <thead>
                                <tr>
                                    <th>Invoice</th>
                                    <th style='text-align: right;'>Due Date</th>
                                    <th style='text-align: right;'>Amount Due</th>
                                </tr>
                            </thead>
                            <tbody>{rows}</tbody>
                        </table>
                        <p style='margin-top: 20px;'>Please arrange payment at your earliest convenience.</p>
                        <p>If you have already made the payment, please disregard this notice.</p>
                        <p>Thank you for your attention to this matter.</p>
                    </div>
                    <div class='footer'>
                        <p><strong>{tenant.CompanyName}</strong></p>
                        {footer}
                        <p>This is an automated message, please do not reply to this email.</p>
                    </div>
                </div>
            </body>
            </html>";
        }

        private static string BuildMkdOverdueHtml(string companyName, List<Invoice> invoices, Tenant tenant)
        {
            string footer = BuildFooterLines(tenant);
            string rows = string.Join("", invoices.Select(i => $@"
                <tr>
                    <td style='padding: 8px 12px; border-bottom: 1px solid #eee;'>{i.InvoiceNumber}</td>
                    <td style='padding: 8px 12px; border-bottom: 1px solid #eee; text-align: right;'>{i.DueDate:MMMM dd, yyyy}</td>
                    <td style='padding: 8px 12px; border-bottom: 1px solid #eee; text-align: right;'>{i.AmountDue:C}</td>
                </tr>"));

            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #B91C1C; color: white; padding: 20px; text-align: center; }}
                    .content {{ padding: 20px; background-color: #f9f9f9; }}
                    .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
                    table {{ width: 100%; border-collapse: collapse; background: white; }}
                    th {{ padding: 10px 12px; background-color: #B91C1C; color: white; text-align: left; }}
                    th:not(:first-child) {{ text-align: right; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>{tenant.CompanyName}</h1>
                        <p>Известување за задоцнети фактури</p>
                    </div>
                    <div class='content'>
                        <p>Почитувани {companyName},</p>
                        <p>Следниве {invoices.Count} фактура/и се <strong>задоцнети</strong>:</p>
                        <table>
                            <thead>
                                <tr>
                                    <th>Фактура</th>
                                    <th style='text-align: right;'>Рок</th>
                                    <th style='text-align: right;'>Износ</th>
                                </tr>
                            </thead>
                            <tbody>{rows}</tbody>
                        </table>
                        <p style='margin-top: 20px;'>Ве молиме извршете ја уплатата во најкраток можен рок.</p>
                        <p>Доколку веќе сте ја извршиле уплатата, занемарете го ова известување.</p>
                        <p>Ви благодариме за вниманието.</p>
                    </div>
                    <div class='footer'>
                        <p><strong>{tenant.CompanyName}</strong></p>
                        {footer}
                        <p>Ова е автоматска порака, ве молиме не одговарајте на оваа е-пошта.</p>
                    </div>
                </div>
            </body>
            </html>";
        }
    }
}