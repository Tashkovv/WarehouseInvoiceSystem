namespace WarehouseInvoiceSystem.Application.Services
{
    using ClosedXML.Excel;
    using WarehouseInvoiceSystem.Application.DTOs.Invoice;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Enums;

    public class ExcelExportService(IInvoiceService invoiceService,
                                    ILocalizationService translations) : IExcelExportService
    {
        private readonly string dateFormat = "dd/MM/yyyy";

        public async Task<byte[]> ExportInvoiceForPrintingAsync(Guid invoiceId)
        {
            InvoiceDto? invoice = await invoiceService.GetInvoiceByIdAsync(invoiceId) ?? throw new KeyNotFoundException($"Invoice with ID {invoiceId} not found");

            using XLWorkbook workbook = new();
            IXLWorksheet worksheet = workbook.Worksheets.Add(translations.GetString("Invoice"));

            // Set page setup for printing
            worksheet.PageSetup.PageOrientation = XLPageOrientation.Portrait;
            worksheet.PageSetup.PaperSize = XLPaperSize.LetterPaper;
            worksheet.PageSetup.Margins.Left = 0.5;
            worksheet.PageSetup.Margins.Right = 0.5;
            worksheet.PageSetup.Margins.Top = 0.75;
            worksheet.PageSetup.Margins.Bottom = 0.75;
            worksheet.PageSetup.CenterHorizontally = true;

            // Company Header (you can customize this)
            worksheet.Range("A1:G1").Merge().Value = translations.GetString("InvoiceSystem");
            worksheet.Range("A1:G1").Style.Font.Bold = true;
            worksheet.Range("A1:G1").Style.Font.FontSize = 18;
            worksheet.Range("A1:G1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Range("A2:G2").Merge().Value = translations.GetString("Brajkovci");
            worksheet.Range("A2:G2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range("A2:G2").Style.Font.FontSize = 10;

            // Invoice title
            int row = 4;
            worksheet.Range($"A{row}:G{row}").Merge().Value = translations.GetString("Invoice").ToUpper();
            worksheet.Range($"A{row}:G{row}").Style.Font.Bold = true;
            worksheet.Range($"A{row}:G{row}").Style.Font.FontSize = 16;
            worksheet.Range($"A{row}:G{row}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range($"A{row}:G{row}").Style.Fill.BackgroundColor = XLColor.LightGray;

            // Invoice Details - Left side
            row += 2;
            int detailsStartRow = row;
            worksheet.Cell(row, 1).Value = invoice.Type.Equals(InvoiceType.Receivable)
                                             ? $"{translations.GetString("BillFrom")}:" 
                                             : $"{translations.GetString("BillTo")}:";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            row++;
            worksheet.Cell(row, 1).Value = invoice.CompanyName;
            worksheet.Cell(row, 1).Style.Font.FontSize = 12;

            // Invoice Details - Right side
            row = detailsStartRow;
            worksheet.Cell(row, 5).Value = $"{translations.GetString("InvoiceNumber")}:";
            worksheet.Cell(row, 5).Style.Font.Bold = true;
            worksheet.Cell(row, 6).Value = invoice.InvoiceNumber;
            worksheet.Range(row, 6, row, 7).Merge();

            row++;
            worksheet.Cell(row, 5).Value = $"{translations.GetString("IssueDate")}:";
            worksheet.Cell(row, 5).Style.Font.Bold = true;
            worksheet.Cell(row, 6).Value = invoice.IssueDate.ToString(dateFormat);
            worksheet.Range(row, 6, row, 7).Merge();

            row++;
            worksheet.Cell(row, 5).Value = $"{translations.GetString("DueDate")}:";
            worksheet.Cell(row, 5).Style.Font.Bold = true;
            worksheet.Cell(row, 6).Value = invoice.DueDate.ToString(dateFormat);
            worksheet.Range(row, 6, row, 7).Merge();

            // Line items table
            row += 3;
            worksheet.Cell(row, 1).Value = "#";
            worksheet.Cell(row, 2).Value = translations.GetString("Description");
            worksheet.Cell(row, 3).Value = translations.GetString("Quantity");
            worksheet.Cell(row, 4).Value = translations.GetString("UnitPrice");
            worksheet.Cell(row, 5).Value = translations.GetString("TaxRate");
            worksheet.Cell(row, 6).Value = translations.GetString("Amount");
            worksheet.Cell(row, 7).Value = translations.GetString("Total");

            IXLRange headerRange = worksheet.Range(row, 1, row, 7);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Line items
            int itemNumber = 1;
            foreach (InvoiceLineDto item in invoice.LineItems)
            {
                row++;
                worksheet.Cell(row, 1).Value = itemNumber++;
                worksheet.Cell(row, 2).Value = item.ProductName;
                worksheet.Cell(row, 3).Value = $"{item.Quantity} {item.ProductUnit}";
                worksheet.Cell(row, 4).Value = item.UnitPrice.ToString("C");
                worksheet.Cell(row, 5).Value = $"{item.TaxRate}%";
                worksheet.Cell(row, 6).Value = item.Amount.ToString("C");
                worksheet.Cell(row, 7).Value = item.TotalAmount.ToString("C");

                IXLRange itemRange = worksheet.Range(row, 1, row, 7);
                itemRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            // Totals section
            row += 2;
            worksheet.Cell(row, 6).Value = $"{translations.GetString("Subtotal")}:";
            worksheet.Cell(row, 7).Value = invoice.SubTotal.ToString("C");
            worksheet.Cell(row, 6).Style.Font.Bold = true;
            worksheet.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            row++;
            worksheet.Cell(row, 6).Value = $"{translations.GetString("Tax")}:";
            worksheet.Cell(row, 7).Value = invoice.TaxAmount.ToString("C");
            worksheet.Cell(row, 6).Style.Font.Bold = true;
            worksheet.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            row++;
            worksheet.Cell(row, 6).Value = $"{translations.GetString("Total").ToUpper()}:";
            worksheet.Cell(row, 7).Value = invoice.TotalAmount.ToString("C");
            worksheet.Range(row, 6, row, 7).Style.Font.Bold = true;
            worksheet.Range(row, 6, row, 7).Style.Font.FontSize = 14;
            worksheet.Range(row, 6, row, 7).Style.Fill.BackgroundColor = XLColor.LightGray;
            worksheet.Range(row, 6, row, 7).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            worksheet.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            row++;
            worksheet.Cell(row, 6).Value = $"{translations.GetString("AmountPaid")}:";
            worksheet.Cell(row, 7).Value = invoice.AmountPaid.ToString("C");
            worksheet.Cell(row, 6).Style.Font.Bold = true;
            worksheet.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            row++;
            worksheet.Cell(row, 6).Value = $"{translations.GetString("AmountDue").ToUpper()}:";
            worksheet.Cell(row, 7).Value = invoice.AmountDue.ToString("C");
            worksheet.Range(row, 6, row, 7).Style.Font.Bold = true;
            worksheet.Range(row, 6, row, 7).Style.Font.FontSize = 14;
            worksheet.Range(row, 6, row, 7).Style.Fill.BackgroundColor = XLColor.Yellow;
            worksheet.Range(row, 6, row, 7).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            worksheet.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            // Notes
            if (!string.IsNullOrWhiteSpace(invoice.Notes))
            {
                row += 3;
                worksheet.Cell(row, 1).Value = $"{translations.GetString("Notes")}:";
                worksheet.Cell(row, 1).Style.Font.Bold = true;
                row++;
                worksheet.Cell(row, 1).Value = invoice.Notes;
                worksheet.Range(row, 1, row, 7).Merge();
                worksheet.Range(row, 1, row, 7).Style.Alignment.WrapText = true;
            }

            // Footer
            row += 3;
            worksheet.Range(row, 1, row, 7).Merge().Value = translations.GetString("ThankYou");
            worksheet.Range(row, 1, row, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range(row, 1, row, 7).Style.Font.Italic = true;

            // Column widths for printing
            worksheet.Column(1).Width = 5;
            worksheet.Column(2).Width = 35;
            worksheet.Column(3).Width = 10;
            worksheet.Column(4).Width = 15;
            worksheet.Column(5).Width = 20;
            worksheet.Column(6).Width = 18;
            worksheet.Column(7).AdjustToContents();

            using MemoryStream stream = new();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> ExportInvoicesToExcelAsync(List<InvoiceDto> invoicesToExport)
        {
            using XLWorkbook workbook = new();
            IXLWorksheet worksheet = workbook.Worksheets.Add(translations.GetString("Invoices"));

            // Headers
            worksheet.Cell(1, 1).Value = translations.GetString("InvoiceNumber");
            worksheet.Cell(1, 2).Value = translations.GetString("Company");
            worksheet.Cell(1, 3).Value = translations.GetString("Type");
            worksheet.Cell(1, 4).Value = translations.GetString("Status");
            worksheet.Cell(1, 5).Value = translations.GetString("IssueDate");
            worksheet.Cell(1, 6).Value = translations.GetString("DueDate");
            worksheet.Cell(1, 7).Value = translations.GetString("Subtotal");
            worksheet.Cell(1, 8).Value = translations.GetString("Tax");
            worksheet.Cell(1, 9).Value = translations.GetString("Total");
            worksheet.Cell(1, 10).Value = translations.GetString("AmountPaid");
            worksheet.Cell(1, 11).Value = translations.GetString("AmountDue");

            IXLRange headerRange = worksheet.Range(1, 1, 1, 11);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // Data
            int row = 2;
            foreach (InvoiceDto invoice in invoicesToExport.OrderByDescending(i => i.CreatedAt))
            {
                worksheet.Cell(row, 1).Value = invoice.InvoiceNumber;
                worksheet.Cell(row, 2).Value = invoice.CompanyName;
                worksheet.Cell(row, 3).Value = translations.GetString(invoice.Type.ToString());
                worksheet.Cell(row, 4).Value = translations.GetString(invoice.Status.ToString());
                worksheet.Cell(row, 5).Value = invoice.IssueDate.ToString(dateFormat);
                worksheet.Cell(row, 6).Value = invoice.DueDate.ToString(dateFormat);
                worksheet.Cell(row, 7).Value = invoice.SubTotal.ToString("C");
                worksheet.Cell(row, 8).Value = invoice.TaxAmount.ToString("C");
                worksheet.Cell(row, 9).Value = invoice.TotalAmount.ToString("C");
                worksheet.Cell(row, 10).Value = invoice.AmountPaid.ToString("C");
                worksheet.Cell(row, 11).Value = invoice.AmountDue.ToString("C");

                row++;
            }

            // Totals
            row++;
            worksheet.Cell(row, 6).Value = $"{translations.GetString("Total").ToUpper()}:";
            worksheet.Cell(row, 6).Style.Font.Bold = true;
            worksheet.Cell(row, 7).Value = invoicesToExport.Sum(i => i.SubTotal).ToString("C");
            worksheet.Cell(row, 8).Value = invoicesToExport.Sum(i => i.TaxAmount).ToString("C");
            worksheet.Cell(row, 9).Value = invoicesToExport.Sum(i => i.TotalAmount).ToString("C");
            worksheet.Cell(row, 10).Value = invoicesToExport.Sum(i => i.AmountPaid).ToString("C");
            worksheet.Cell(row, 11).Value = invoicesToExport.Sum(i => i.AmountDue).ToString("C");

            IXLRange totalsRange = worksheet.Range(row, 6, row, 11);
            totalsRange.Style.Font.Bold = true;
            totalsRange.Style.Fill.BackgroundColor = XLColor.LightYellow;
            totalsRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            using MemoryStream stream = new();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> ExportInvoicesByDateRangeAsync(List<InvoiceDto> invoicesToExport, DateTime startDate, DateTime endDate)
        {
            string headerName = "A1:K1";
            List<InvoiceDto> filteredInvoices = invoicesToExport
                .Where(i => i.IssueDate >= startDate && i.IssueDate <= endDate)
                .OrderBy(i => i.IssueDate)
                .ToList();

            using XLWorkbook workbook = new();
            IXLWorksheet worksheet = workbook.Worksheets.Add(translations.GetString("Invoices"));

            // Title
            if (translations.CurrentLanguage == "en")
            {
                worksheet.Range(headerName).Merge().Value = $"Invoices from {startDate:MM/dd/yyyy} to {endDate:MM/dd/yyyy}";
            }
            else
            {
                worksheet.Range(headerName).Merge().Value = $"Фактури од {startDate:MM/dd/yyyy} до {endDate:MM/dd/yyyy}";
            }
            worksheet.Range(headerName).Style.Font.Bold = true;
            worksheet.Range(headerName).Style.Font.FontSize = 14;
            worksheet.Range(headerName).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Headers
            worksheet.Cell(3, 1).Value = translations.GetString("InvoiceNumber");
            worksheet.Cell(3, 2).Value = translations.GetString("Company");
            worksheet.Cell(3, 3).Value = translations.GetString("Type");
            worksheet.Cell(3, 4).Value = translations.GetString("Status");
            worksheet.Cell(3, 5).Value = translations.GetString("IssueDate");
            worksheet.Cell(3, 6).Value = translations.GetString("DueDate");
            worksheet.Cell(3, 7).Value = translations.GetString("Subtotal");
            worksheet.Cell(3, 8).Value = translations.GetString("Tax");
            worksheet.Cell(3, 9).Value = translations.GetString("Total");
            worksheet.Cell(3, 10).Value = translations.GetString("AmountPaid");
            worksheet.Cell(3, 11).Value = translations.GetString("AmountDue");

            IXLRange headerRange = worksheet.Range(3, 1, 3, 11);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // Data
            int row = 4;
            foreach (InvoiceDto invoice in filteredInvoices)
            {
                worksheet.Cell(row, 1).Value = invoice.InvoiceNumber;
                worksheet.Cell(row, 2).Value = invoice.CompanyName;
                worksheet.Cell(row, 3).Value = translations.GetString(invoice.Type.ToString());
                worksheet.Cell(row, 4).Value = translations.GetString(invoice.Status.ToString());
                worksheet.Cell(row, 5).Value = invoice.IssueDate.ToString(dateFormat);
                worksheet.Cell(row, 6).Value = invoice.DueDate.ToString(dateFormat);
                worksheet.Cell(row, 7).Value = invoice.SubTotal.ToString("C");
                worksheet.Cell(row, 8).Value = invoice.TaxAmount.ToString("C");
                worksheet.Cell(row, 9).Value = invoice.TotalAmount.ToString("C");
                worksheet.Cell(row, 10).Value = invoice.AmountPaid.ToString("C");
                worksheet.Cell(row, 11).Value = invoice.AmountDue.ToString("C");

                row++;
            }

            // Totals
            row++;
            worksheet.Cell(row, 6).Value = $"{translations.GetString("Total").ToUpper()}:";
            worksheet.Cell(row, 6).Style.Font.Bold = true;
            worksheet.Cell(row, 7).Value = filteredInvoices.Sum(i => i.SubTotal).ToString("C");
            worksheet.Cell(row, 8).Value = filteredInvoices.Sum(i => i.TaxAmount).ToString("C");
            worksheet.Cell(row, 9).Value = filteredInvoices.Sum(i => i.TotalAmount).ToString("C");
            worksheet.Cell(row, 10).Value = filteredInvoices.Sum(i => i.AmountPaid).ToString("C");
            worksheet.Cell(row, 11).Value = filteredInvoices.Sum(i => i.AmountDue).ToString("C");

            IXLRange totalsRange = worksheet.Range(row, 6, row, 11);
            totalsRange.Style.Font.Bold = true;
            totalsRange.Style.Fill.BackgroundColor = XLColor.LightYellow;
            totalsRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            using MemoryStream stream = new();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
