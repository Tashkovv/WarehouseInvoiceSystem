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

        public async Task<bool> SendInvoiceEmailAsync(Guid invoiceId, string? customMessage = null)
        {
            try
            {
                InvoiceDto invoice = await invoiceService.GetInvoiceByIdAsync(invoiceId)
                    ?? throw new KeyNotFoundException($"Invoice with ID {invoiceId} not found");

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
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending invoice email: {ex.Message}");
                return false;
            }
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
    }
}