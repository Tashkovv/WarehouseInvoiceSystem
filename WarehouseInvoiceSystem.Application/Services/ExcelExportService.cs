namespace WarehouseInvoiceSystem.Application.Services
{
    using ClosedXML.Excel;
    using ClosedXML.Excel.Drawings;
    using WarehouseInvoiceSystem.Application.DTOs.Invoice;
    using WarehouseInvoiceSystem.Application.DTOs.PurchaseNote;
    using WarehouseInvoiceSystem.Application.DTOs.Tenant;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Application.Models;
    using WarehouseInvoiceSystem.Domain.Enums;

    public class ExcelExportService(IInvoiceService invoiceService,
                                    ILocalizationService translations,
                                    ITenantService tenantService) : IExcelExportService
    {
        private readonly string dateFormat = "dd/MM/yyyy";

        private const string totalString = "Total";

        public Task<byte[]> ExportListToExcelAsync<T>(IEnumerable<T> data, IReadOnlyList<ExportColumn<T>> columns, ExportListOptions options)
        {
            using XLWorkbook workbook = new();
            IXLWorksheet ws = workbook.Worksheets.Add(options.SheetName);

            int row = 1;

            if (options.Title is not null)
                row = WriteTitleRow(ws, options.Title, columns.Count, row);

            if (options.SubtitleLines is { Count: > 0 })
                row = WriteSubtitleLines(ws, options.SubtitleLines, columns.Count, row);

            row = WriteHeaderRow(ws, columns, row);

            List<T> items = [.. data];
            foreach (T item in items)
            {
                row++;
                for (int i = 0; i < columns.Count; i++)
                    SetCellValue(ws.Cell(row, i + 1), columns[i].Selector(item), columns[i].ColumnType);
            }

            if (columns.Any(c => c.IncludeInTotals))
                WriteTotalsRow(ws, items, columns, row + 2);

            ws.Columns().AdjustToContents();
            return Task.FromResult(WorkbookToBytes(workbook));
        }

        private static int WriteTitleRow(IXLWorksheet ws, string title, int colCount, int row)
        {
            IXLRange range = ws.Range(row, 1, row, colCount).Merge();
            range.Value = title;
            range.Style.Font.Bold = true;
            range.Style.Font.FontSize = 14;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            return row + 2;
        }

        private static int WriteSubtitleLines(IXLWorksheet ws, IReadOnlyList<string> lines, int colCount, int row)
        {
            foreach (string line in lines)
            {
                IXLRange range = ws.Range(row, 1, row, colCount).Merge();
                range.Value = line;
                range.Style.Font.FontSize = 10;
                row++;
            }
            return row + 1;
        }

        private static int WriteHeaderRow<T>(IXLWorksheet ws, IReadOnlyList<ExportColumn<T>> columns, int row)
        {
            for (int i = 0; i < columns.Count; i++)
                ws.Cell(row, i + 1).Value = columns[i].Header;

            IXLRange headerRange = ws.Range(row, 1, row, columns.Count);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            return row;
        }

        private void SetCellValue(IXLCell cell, object? value, ExportColumnType columnType)
        {
            cell.Value = FormatValue(value, columnType);

            if (columnType is ExportColumnType.Currency or ExportColumnType.Number)
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        }

        private string FormatValue(object? value, ExportColumnType columnType) => columnType switch
        {
            ExportColumnType.Currency => value is decimal d ? d.ToString("C") : value?.ToString() ?? string.Empty,
            ExportColumnType.Date => value is DateTime dt ? dt.ToString(dateFormat) : value?.ToString() ?? string.Empty,
            ExportColumnType.Number => value is decimal n ? n.ToString("N2") : value?.ToString() ?? string.Empty,
            _ => value?.ToString() ?? string.Empty,
        };

        private static void WriteTotalsRow<T>(IXLWorksheet ws, List<T> items, IReadOnlyList<ExportColumn<T>> columns, int row)
        {
            for (int i = 0; i < columns.Count; i++)
            {
                if (!columns[i].IncludeInTotals) continue;

                decimal total = items.Sum(item =>
                    columns[i].Selector(item) is decimal dv ? dv : 0m);

                IXLCell cell = ws.Cell(row, i + 1);
                cell.Value = columns[i].ColumnType == ExportColumnType.Currency
                    ? total.ToString("C")
                    : total.ToString("N2");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            }

            IXLRange totalsRange = ws.Range(row, 1, row, columns.Count);
            totalsRange.Style.Font.Bold = true;
            totalsRange.Style.Fill.BackgroundColor = XLColor.LightYellow;
            totalsRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        }

        public async Task<byte[]> ExportInvoiceForPrintingAsync(Guid invoiceId)
        {
            InvoiceDto invoice = await invoiceService.GetInvoiceByIdAsync(invoiceId)
                ?? throw new KeyNotFoundException($"Invoice with ID {invoiceId} not found");
            return await ExportInvoiceForPrintingAsync(invoice);
        }

        public async Task<byte[]> ExportInvoiceForPrintingAsync(InvoiceDto invoice)
        {
            TenantDto tenant = await tenantService.GetAsync();
            return ExportInvoiceForPrinting(invoice, tenant);
        }

        private byte[] ExportInvoiceForPrinting(InvoiceDto invoice, TenantDto tenant)
        {
            using XLWorkbook workbook = new();
            IXLWorksheet ws = workbook.Worksheets.Add(translations.GetString("Invoice"));

            bool hasDiscount = invoice.DiscountTotal > 0;
            int totalCols = hasDiscount ? 7 : 6; // #, Description, Quantity, UnitPrice, TaxRate, [Discount], Amount

            // ── Page setup ───────────────────────────────────────────────────────
            ws.PageSetup.PageOrientation = XLPageOrientation.Portrait;
            ws.PageSetup.PaperSize = XLPaperSize.LetterPaper;
            ws.PageSetup.Margins.Left = 0.5;
            ws.PageSetup.Margins.Right = 0.5;
            ws.PageSetup.Margins.Top = 0.75;
            ws.PageSetup.Margins.Bottom = 0.75;
            ws.PageSetup.CenterHorizontally = true;

            // ── Header area ──────────────────────────────────────────────────────
            int row = 1;

            // Company name (centered)
            ws.Range(row, 1, row, totalCols).Merge().Value = tenant.CompanyName;
            ws.Range(row, 1, row, totalCols).Style.Font.Bold = true;
            ws.Range(row, 1, row, totalCols).Style.Font.FontSize = 18;
            ws.Range(row, 1, row, totalCols).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            row++;
            // Company address (centered)
            ws.Range(row, 1, row, totalCols).Merge().Value = tenant.Address ?? string.Empty;
            ws.Range(row, 1, row, totalCols).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(row, 1, row, totalCols).Style.Font.FontSize = 10;

            // Logo (top-right)
            if (tenant.LogoData is { Length: > 0 })
            {
                using MemoryStream logoStream = new(tenant.LogoData);
                ws.AddPicture(logoStream)
                    .MoveTo(ws.Cell("F1"))
                    .WithSize(80, 40);
            }

            // ── Title ────────────────────────────────────────────────────────────
            row += 2;
            ws.Range(row, 1, row, totalCols).Merge().Value = translations.GetString("Invoice").ToUpper();
            ws.Range(row, 1, row, totalCols).Style.Font.Bold = true;
            ws.Range(row, 1, row, totalCols).Style.Font.FontSize = 16;
            ws.Range(row, 1, row, totalCols).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(row, 1, row, totalCols).Style.Fill.BackgroundColor = XLColor.LightGray;

            // ── Invoice details ──────────────────────────────────────────────────
            row += 2;
            int detailsStartRow = row;

            // Left side: Bill From/To + company name
            ws.Cell(row, 1).Value = invoice.Type == InvoiceType.Receivable
                ? $"{translations.GetString("BillFrom")}:"
                : $"{translations.GetString("BillTo")}:";
            ws.Cell(row, 1).Style.Font.Bold = true;

            row++;
            ws.Cell(row, 1).Value = invoice.CompanyName;
            ws.Cell(row, 1).Style.Font.FontSize = 12;

            // Right side: invoice number, issue date, due date
            row = detailsStartRow;
            WriteDetailRow(ws, row, 5, translations.GetString("InvoiceNumber"), invoice.InvoiceNumber);
            row++;
            WriteDetailRow(ws, row, 5, translations.GetString("IssueDate"), invoice.IssueDate.ToString(dateFormat));
            row++;
            WriteDetailRow(ws, row, 5, translations.GetString("DueDate"), invoice.DueDate.ToString(dateFormat));

            // ── Line items table ─────────────────────────────────────────────────
            row += 3;

            ws.Cell(row, 1).Value = "#";
            ws.Cell(row, 2).Value = translations.GetString("Product");
            ws.Cell(row, 3).Value = translations.GetString("Quantity");
            ws.Cell(row, 4).Value = translations.GetString("UnitPrice");
            ws.Cell(row, 5).Value = translations.GetString("TaxRate");
            if (hasDiscount)
                ws.Cell(row, 6).Value = translations.GetString("Discount");
            ws.Cell(row, hasDiscount ? 7 : 6).Value = translations.GetString("Amount");

            IXLRange headerRange = ws.Range(row, 1, row, totalCols);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Data rows
            int itemNumber = 1;
            foreach (InvoiceLineDto line in invoice.LineItems)
            {
                row++;
                ws.Cell(row, 1).Value = itemNumber++;
                ws.Cell(row, 2).Value = line.ProductName;
                ws.Cell(row, 3).Value = $"{line.Quantity} {line.ProductUnit}";
                ws.Cell(row, 4).Value = line.UnitPrice.ToString("C");
                ws.Cell(row, 5).Value = $"{line.TaxRate}%";
                if (hasDiscount)
                    ws.Cell(row, 6).Value = line.DiscountPercentage > 0 ? $"-{line.DiscountAmount.ToString("C")}" : "";
                ws.Cell(row, hasDiscount ? 7 : 6).Value = line.Amount.ToString("C");

                ws.Range(row, 1, row, totalCols).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            // ── Totals section ───────────────────────────────────────────────────
            int labelCol = totalCols - 1;
            row += 2;
            WriteTotalLine(ws, row, labelCol, $"{translations.GetString("Subtotal")}:", invoice.SubTotal.ToString("C"));
            if (hasDiscount)
            {
                row++;
                WriteTotalLine(ws, row, labelCol, $"{translations.GetString("Discount")}:", $"-{invoice.DiscountTotal.ToString("C")}");
            }
            row++;
            WriteTotalLine(ws, row, labelCol, $"{translations.GetString("Tax")}:", invoice.TaxAmount.ToString("C"));

            // Grand total (highlighted)
            row++;
            WriteTotalLine(ws, row, labelCol, $"{translations.GetString(totalString).ToUpper()}:", invoice.TotalAmount.ToString("C"));
            ws.Range(row, labelCol, row, totalCols).Style.Font.FontSize = 14;
            ws.Range(row, labelCol, row, totalCols).Style.Fill.BackgroundColor = XLColor.LightGray;
            ws.Range(row, labelCol, row, totalCols).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

            row++;
            WriteTotalLine(ws, row, labelCol, $"{translations.GetString("AmountPaid")}:", invoice.AmountPaid.ToString("C"));

            // Amount due (highlighted)
            row++;
            WriteTotalLine(ws, row, labelCol, $"{translations.GetString("AmountDue").ToUpper()}:", invoice.AmountDue.ToString("C"));
            ws.Range(row, labelCol, row, totalCols).Style.Font.FontSize = 14;
            ws.Range(row, labelCol, row, totalCols).Style.Fill.BackgroundColor = XLColor.Yellow;
            ws.Range(row, labelCol, row, totalCols).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

            // ── Notes ────────────────────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(invoice.Notes))
            {
                row += 3;
                ws.Cell(row, 1).Value = $"{translations.GetString("Notes")}:";
                ws.Cell(row, 1).Style.Font.Bold = true;
                row++;
                ws.Range(row, 1, row, totalCols).Merge().Value = invoice.Notes;
                ws.Range(row, 1, row, totalCols).Style.Alignment.WrapText = true;
            }

            // ── Footer ──────────────────────────────────────────────────────────
            row += 3;
            ws.Range(row, 1, row, totalCols).Merge().Value = translations.GetString("ThankYou");
            ws.Range(row, 1, row, totalCols).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(row, 1, row, totalCols).Style.Font.Italic = true;

            // ── Column widths ────────────────────────────────────────────────────
            ws.Column(1).Width = 5;
            ws.Column(2).Width = 35;
            ws.Column(3).Width = 10;
            ws.Column(4).Width = 15;
            ws.Column(5).Width = 20;
            ws.Column(6).Width = 18;
            ws.Column(7).AdjustToContents();

            return WorkbookToBytes(workbook);
        }

        private static void WriteDetailRow(IXLWorksheet ws, int row, int labelCol, string label, string value)
        {
            ws.Cell(row, labelCol).Value = $"{label}:";
            ws.Cell(row, labelCol).Style.Font.Bold = true;
            ws.Cell(row, labelCol + 1).Value = value;
            ws.Range(row, labelCol + 1, row, labelCol + 2).Merge();
        }

        private static void WriteTotalLine(IXLWorksheet ws, int row, int labelCol, string label, string value)
        {
            ws.Cell(row, labelCol).Value = label;
            ws.Cell(row, labelCol + 1).Value = value;
            ws.Cell(row, labelCol).Style.Font.Bold = true;
            ws.Cell(row, labelCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Range(row, labelCol, row, labelCol + 1).Style.Font.Bold = true;
        }

        public async Task<byte[]> ExportInvoicesToExcelAsync(IEnumerable<InvoiceDto> invoicesToExport)
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
            worksheet.Cell(1, 9).Value = translations.GetString(totalString);
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
            worksheet.Cell(row, 6).Value = $"{translations.GetString(totalString).ToUpper()}:";
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

            return WorkbookToBytes(workbook);
        }

        public async Task<byte[]> ExportInvoicesByDateRangeAsync(List<InvoiceDto> invoicesToExport, DateTime startDate, DateTime endDate)
        {
            string headerName = "A1:K1";
            List<InvoiceDto> filteredInvoices = [.. invoicesToExport
                .Where(i => i.IssueDate >= startDate && i.IssueDate <= endDate)
                .OrderBy(i => i.IssueDate)];

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
            worksheet.Cell(3, 9).Value = translations.GetString(totalString);
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
            worksheet.Cell(row, 6).Value = $"{translations.GetString(totalString).ToUpper()}:";
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

            return WorkbookToBytes(workbook);
        }

        public async Task<byte[]> ExportPurchaseNoteForPrintingAsync(PurchaseNoteDto purchaseNote)
        {
            TenantDto tenant = await tenantService.GetAsync();
            return ExportPurchaseNoteForPrinting(purchaseNote, tenant);
        }

        private byte[] ExportPurchaseNoteForPrinting(PurchaseNoteDto purchaseNote, TenantDto tenant)
        {
            using XLWorkbook workbook = new();
            IXLWorksheet ws = workbook.Worksheets.Add(translations.GetString("PurchaseNote"));

            // Page setup
            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
            ws.PageSetup.Margins.Left = 0.5;
            ws.PageSetup.Margins.Right = 0.5;
            ws.PageSetup.Margins.Top = 0.5;
            ws.PageSetup.Margins.Bottom = 0.5;
            ws.PageSetup.CenterHorizontally = true;

            int totalCols = 8; // Ред.Број, Наименование, Ед.мера, Примено, Кало%, Нето, Един.цена, Износ

            // ── Header area ─────────────────────────────────────────────────────
            int row = 1;

            // Row 1: Company name (left) | Date (right)
            ws.Range(row, 1, row, 4).Merge().Value = tenant.CompanyName;
            ws.Range(row, 1, row, 4).Style.Font.Bold = true;
            ws.Range(row, 1, row, 4).Style.Font.FontSize = 11;

            // Logo (top-left, above company)
            if (tenant.LogoData is { Length: > 0 })
            {
                using MemoryStream logoStream = new(tenant.LogoData);
                ws.AddPicture(logoStream)
                    .MoveTo(ws.Cell("A1"))
                    .WithSize(80, 40);
            }

            ws.Range(row, 6, row, totalCols).Merge().Value = $"На ден {purchaseNote.PurchaseDate:dd.MM.yyyy} год.";
            ws.Range(row, 6, row, totalCols).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            // Row 2: Company address (left)
            row++;
            ws.Range(row, 1, row, 4).Merge().Value = tenant.Address ?? string.Empty;
            ws.Range(row, 1, row, 4).Style.Font.FontSize = 9;

            // Row 3: empty gap
            row++;

            // Row 4: Title + Note number (centered)
            row++;
            ws.Range(row, 1, row, totalCols).Merge().Value = $"ОТКУПНА БЕЛЕШКА  Бр. {purchaseNote.NoteNumber}";
            ws.Range(row, 1, row, totalCols).Style.Font.Bold = true;
            ws.Range(row, 1, row, totalCols).Style.Font.FontSize = 14;
            ws.Range(row, 1, row, totalCols).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Row 5: Individual name (left) | ЕМБГ (right)
            row++;
            ws.Range(row, 1, row, 5).Merge().Value = $"Денес откупив од лицето {purchaseNote.IndividualFullName}";
            ws.Range(row, 1, row, 5).Style.Font.FontSize = 10;
            ws.Range(row, 6, row, totalCols).Merge().Value = $"ЕМБГ: {purchaseNote.IndividualIdentificationNumber}";
            ws.Range(row, 6, row, totalCols).Style.Font.FontSize = 10;
            ws.Range(row, 6, row, totalCols).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            // Row 6: Address (left) | Трансакциска сметка (right)
            row++;
            string addressLine = !string.IsNullOrWhiteSpace(purchaseNote.IndividualAddress)
                ? $"Од {purchaseNote.IndividualAddress}  долу наведените производи:"
                : "долу наведените производи:";
            ws.Range(row, 1, row, 5).Merge().Value = addressLine;
            ws.Range(row, 1, row, 5).Style.Font.FontSize = 10;
            if (!string.IsNullOrWhiteSpace(purchaseNote.IndividualBankAccount))
            {
                ws.Range(row, 6, row, totalCols).Merge().Value = $"Трансакциска сметка: {purchaseNote.IndividualBankAccount}";
                ws.Range(row, 6, row, totalCols).Style.Font.FontSize = 10;
                ws.Range(row, 6, row, totalCols).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            }

            // ── Line items table ─────────────────────────────────────────────────
            row += 2;
            int headerRow = row;

            // Header row
            ws.Cell(row, 1).Value = "Ред.\nБрој";
            ws.Cell(row, 2).Value = "Наименование";
            ws.Cell(row, 3).Value = "Ед.\nмера";
            ws.Cell(row, 4).Value = "Примено";
            ws.Cell(row, 5).Value = "Се одбива\nкало %";
            ws.Cell(row, 6).Value = "Нето";
            ws.Cell(row, 7).Value = "Един.\nцена";
            ws.Cell(row, 8).Value = "Износ\nден.";

            // Style header row
            IXLRange headerRange = ws.Range(headerRow, 1, row, totalCols);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Font.FontSize = 9;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            headerRange.Style.Alignment.WrapText = true;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            ws.Row(headerRow).Height = 35;

            // Data rows
            int itemNumber = 1;
            foreach (PurchaseNoteLineDto line in purchaseNote.LineItems)
            {
                row++;
                ws.Cell(row, 1).Value = itemNumber++;
                ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(row, 2).Value = line.ProductName;
                ws.Cell(row, 3).Value = line.ProductUnit;
                ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(row, 4).Value = line.GrossQuantity;
                ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 5).Value = line.KaloPercentage;
                ws.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 6).Value = line.Quantity;
                ws.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 7).Value = line.UnitPrice;
                ws.Cell(row, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 8).Value = line.Amount;
                ws.Cell(row, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0";

                IXLRange rowRange = ws.Range(row, 1, row, totalCols);
                rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Row(row).Height = 30;
            }

            // Ensure minimum 3 data rows
            int minDataRows = 3;
            int dataRowsWritten = purchaseNote.LineItems.Count;
            for (int i = dataRowsWritten; i < minDataRows; i++)
            {
                row++;
                IXLRange emptyRow = ws.Range(row, 1, row, totalCols);
                emptyRow.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                emptyRow.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                ws.Row(row).Height = 30;
            }

            // ── Totals row ─────────────────────────────────────────────────────────
            row++;
            ws.Range(row, 1, row, 7).Merge().Value = "Вкупно";
            ws.Range(row, 1, row, 7).Style.Font.Bold = true;
            ws.Range(row, 1, row, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            decimal totalAmount = purchaseNote.LineItems.Sum(l => l.Amount);
            ws.Cell(row, 8).Value = totalAmount;
            ws.Cell(row, 8).Style.Font.Bold = true;
            ws.Cell(row, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0";
            IXLRange totalRow = ws.Range(row, 1, row, totalCols);
            totalRow.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            totalRow.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Table outside border
            IXLRange tableRange = ws.Range(headerRow, 1, row, totalCols);
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

            // ── Footer area ──────────────────────────────────────────────────────
            row += 2;
            ws.Range(row, 1, row, totalCols).Merge().Value = "Со букви исплатено ден. _______________________________________________________________";
            ws.Range(row, 1, row, totalCols).Style.Font.FontSize = 10;

            // Signature blocks — single row with 4 columns, all centered
            row += 2;
            ws.Range(row, 1, row, 2).Merge().Value = "Примил стоката,";
            ws.Range(row, 1, row, 2).Style.Font.FontSize = 10;
            ws.Range(row, 1, row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(row, 3, row, 4).Merge().Value = "Исплатил,";
            ws.Range(row, 3, row, 4).Style.Font.FontSize = 10;
            ws.Range(row, 3, row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(row, 5, row, 6).Merge().Value = "Примил пари,";
            ws.Range(row, 5, row, 6).Style.Font.FontSize = 10;
            ws.Range(row, 5, row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(row, 7, row, 8).Merge().Value = "Предал стоката,";
            ws.Range(row, 7, row, 8).Style.Font.FontSize = 10;
            ws.Range(row, 7, row, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Signature lines — centered under each label
            row += 2;
            ws.Range(row, 1, row, 2).Merge().Value = "_______________";
            ws.Range(row, 1, row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(row, 3, row, 4).Merge().Value = "_______________";
            ws.Range(row, 3, row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(row, 5, row, 6).Merge().Value = "_______________";
            ws.Range(row, 5, row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(row, 7, row, 8).Merge().Value = "_______________";
            ws.Range(row, 7, row, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Column widths
            ws.Column(1).Width = 5;    // Ред. Број
            ws.Column(2).Width = 25;   // Наименование
            ws.Column(3).Width = 7;    // Ед. мера
            ws.Column(4).Width = 12;   // Примено
            ws.Column(5).Width = 10;   // Кало %
            ws.Column(6).Width = 12;   // Нето
            ws.Column(7).Width = 10;   // Един. цена
            ws.Column(8).Width = 14;   // Износ ден.

            return WorkbookToBytes(workbook);
        }

        private static byte[] WorkbookToBytes(XLWorkbook workbook)
        {
            using MemoryStream stream = new();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}