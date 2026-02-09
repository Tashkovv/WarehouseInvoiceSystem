namespace WarehouseInvoiceSystem.Application.Services
{
    using MailKit.Net.Smtp;
    using MailKit.Security;
    using Microsoft.Extensions.Options;
    using MimeKit;
    using WarehouseInvoiceSystem.Application.DTOs.Invoice;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Application.Models;
    using WarehouseInvoiceSystem.Domain.Invoice.Enums;

    public class EmailService(
        IOptions<EmailSettings> emailSettings,
        IInvoiceService invoiceService,
        IExcelExportService excelExportService,
        ILocalizationService translations) : IEmailService
    {
        private readonly EmailSettings emailSettings = emailSettings.Value;

        public async Task<bool> SendInvoiceEmailAsync(Guid invoiceId, string? customMessage = null)
        {
            try
            {
                InvoiceDto? invoice = await invoiceService.GetInvoiceByIdAsync(invoiceId) ?? throw new KeyNotFoundException($"Invoice with ID {invoiceId} not found");

                // Create email message
                MimeMessage message = new();
                message.From.Add(new MailboxAddress(emailSettings.SenderName, emailSettings.SenderEmail));
                message.To.Add(new MailboxAddress(invoice.CompanyName, invoice.CompanyEmail));
                message.Subject = $"{translations.GetString("Invoice")} {invoice.InvoiceNumber} {translations.GetString("From")} {emailSettings.SenderName}";

                // Build email body
                BodyBuilder bodyBuilder = new()
                {
                    HtmlBody = translations.CurrentLanguage.Equals("mk")
                               ? BuildMkdInvoiceEmailHtml(invoice, customMessage)
                               : BuildEnglishInvoiceEmailHtml(invoice, customMessage)
                };

                // Attach invoice as Excel (print-ready format)
                byte[] invoiceExcel = await excelExportService.ExportInvoiceForPrintingAsync(invoiceId);
                bodyBuilder.Attachments.Add($"{translations.GetString("Invoice")}_{invoice.InvoiceNumber}.xlsx", invoiceExcel);

                message.Body = bodyBuilder.ToMessageBody();

                // Send email
                using SmtpClient client = new();
                await client.ConnectAsync(emailSettings.SmtpServer, emailSettings.SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(emailSettings.Username, emailSettings.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

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
                MimeMessage message = new();
                message.From.Add(new MailboxAddress(emailSettings.SenderName, emailSettings.SenderEmail));
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

                using SmtpClient client = new();
                await client.ConnectAsync(emailSettings.SmtpServer, emailSettings.SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(emailSettings.Username, emailSettings.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending test email: {ex.Message}");
                return false;
            }
        }

        private string BuildMkdInvoiceEmailHtml(InvoiceDto invoice, string? customMessage)
        {
            string invoiceType = invoice.Type == InvoiceType.Receivable ? "Фактура" : "Сметка";
            string greeting = invoice.Type == InvoiceType.Receivable
                ? $"Почитувани {invoice.CompanyName},"
                : $"Почитувани,";

            string customMessageSection = !string.IsNullOrWhiteSpace(customMessage)
                ? $"<p>{customMessage}</p>"
                : "";

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
                    .button {{ display: inline-block; padding: 12px 24px; background-color: #1E40AF; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>{emailSettings.SenderName}</h1>
                        <p>Систем за фактури</p>
                    </div>
                    
                    <div class='content'>
                        <h2>{invoiceType} {invoice.InvoiceNumber}</h2>
                        <p>{greeting}</p>
                        
                        {customMessageSection}
                        
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
                                <span class='amount'>{invoice.TotalAmount.ToString("C")}</span>
                            </div>
                        </div>
                        
                        <p>Доколку имате какви било прашања во врска со оваа {invoiceType.ToLower()}, слободно контактирајте не.</p>
                        
                        <p>Ви благодариме за соработката!</p>
                    </div>
                    
                    <div class='footer'>
                        <p><strong>{emailSettings.SenderName}</strong></p>
                        <p>Брајковци, Валандово, Македонија</p>
                        <p>Ова е автоматска порака, ве молиме не одговарајте на оваа е-пошта.</p>
                    </div>
                </div>
            </body>
            </html>";
        }

        private string BuildEnglishInvoiceEmailHtml(InvoiceDto invoice, string? customMessage)
        {
            string invoiceType = invoice.Type == InvoiceType.Receivable ? "Invoice" : "Bill";
            string greeting = invoice.Type == InvoiceType.Receivable
                ? $"Dear {invoice.CompanyName},"
                : $"Dear Team,";

            string customMessageSection = !string.IsNullOrWhiteSpace(customMessage)
                ? $"<p>{customMessage}</p>"
                : "";

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
                    .button {{ display: inline-block; padding: 12px 24px; background-color: #1E40AF; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>{emailSettings.SenderName}</h1>
                        <p>Invoice System</p>
                    </div>
                    
                    <div class='content'>
                        <h2>{invoiceType} {invoice.InvoiceNumber}</h2>
                        <p>{greeting}</p>
                        
                        {customMessageSection}
                        
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
                        <p><strong>{emailSettings.SenderName}</strong></p>
                        <p>Strumica, Macedonia</p>
                        <p>This is an automated message, please do not reply to this email.</p>
                    </div>
                </div>
            </body>
            </html>";
        }
    }
}
