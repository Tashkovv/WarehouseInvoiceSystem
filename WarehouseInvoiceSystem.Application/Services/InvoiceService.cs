namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.DTOs.Invoice;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Invoice.Domain;
    using WarehouseInvoiceSystem.Domain.Invoice.Enums;
    using WarehouseInvoiceSystem.Domain.InvoiceLine.Domain;

    public class InvoiceService(IInvoiceRepository invoiceRepository, ICompanyRepository companyRepository) : IInvoiceService
    {
        public async Task<IEnumerable<InvoiceDto>> GetAllInvoicesAsync()
        {
            IEnumerable<Invoice> invoices = await invoiceRepository.GetAllAsync();
            return invoices.Select(MapToDto);
        }

        public async Task<IEnumerable<InvoiceDto>> GetInvoicesByCompanyAsync(int companyId)
        {
            IEnumerable<Invoice> invoices = await invoiceRepository.GetByCompanyIdAsync(companyId);
            return invoices.Select(MapToDto);
        }

        public async Task<IEnumerable<InvoiceDto>> GetInvoicesByTypeAsync(InvoiceType type)
        {
            IEnumerable<Invoice> invoices = await invoiceRepository.GetByTypeAsync(type);
            return invoices.Select(MapToDto);
        }

        public async Task<IEnumerable<InvoiceDto>> GetInvoicesByStatusAsync(InvoiceStatus status)
        {
            IEnumerable<Invoice> invoices = await invoiceRepository.GetByStatusAsync(status);
            return invoices.Select(MapToDto);
        }

        public async Task<IEnumerable<InvoiceDto>> GetOverdueInvoicesAsync()
        {
            IEnumerable<Invoice> invoices = await invoiceRepository.GetOverdueInvoicesAsync();
            return invoices.Select(MapToDto);
        }

        public async Task<InvoiceDto?> GetInvoiceByIdAsync(int id)
        {
            Invoice? invoice = await invoiceRepository.GetByIdAsync(id);
            return invoice == null ? null : MapToDto(invoice);
        }

        public async Task<InvoiceDto?> GetInvoiceByNumberAsync(string invoiceNumber)
        {
            Invoice? invoice = await invoiceRepository.GetByInvoiceNumberAsync(invoiceNumber);
            return invoice == null ? null : MapToDto(invoice);
        }

        public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto createDto)
        {
            // Validate company exists
            if (!await companyRepository.ExistsAsync(createDto.CompanyId))
                throw new KeyNotFoundException($"Company with ID {createDto.CompanyId} not found");

            // Generate invoice number
            string invoiceNumber = await invoiceRepository.GenerateInvoiceNumberAsync(createDto.Type);

            // Calculate totals
            List<InvoiceLine> lineItems = createDto.LineItems.Select(li => new InvoiceLine
            {
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                TaxRate = li.TaxRate
            }).ToList();

            decimal subTotal = lineItems.Sum(li => li.Amount);
            decimal taxAmount = lineItems.Sum(li => li.TaxAmount);
            decimal totalAmount = lineItems.Sum(li => li.TotalAmount);

            Invoice invoice = new()
            {
                InvoiceNumber = invoiceNumber,
                CompanyId = createDto.CompanyId,
                Type = createDto.Type,
                Status = InvoiceStatus.Draft,
                IssueDate = createDto.IssueDate,
                DueDate = createDto.DueDate,
                SubTotal = subTotal,
                TaxAmount = taxAmount,
                TotalAmount = totalAmount,
                AmountPaid = 0,
                Notes = createDto.Notes,
                LineItems = lineItems
            };

            Invoice created = await invoiceRepository.CreateAsync(invoice);
            return MapToDto(created);
        }

        public async Task<InvoiceDto> UpdateInvoiceAsync(int id, UpdateInvoiceDto updateDto)
        {
            Invoice? invoice = await invoiceRepository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Invoice with ID {id} not found");

            // Validate company exists
            if (!await companyRepository.ExistsAsync(updateDto.CompanyId))
                throw new KeyNotFoundException($"Company with ID {updateDto.CompanyId} not found");

            // Update basic properties
            invoice.CompanyId = updateDto.CompanyId;
            invoice.Type = updateDto.Type;
            invoice.Status = updateDto.Status;
            invoice.IssueDate = updateDto.IssueDate;
            invoice.DueDate = updateDto.DueDate;
            invoice.Notes = updateDto.Notes;

            // Update line items
            // Remove old items
            invoice.LineItems.Clear();

            // Add new/updated items
            List<InvoiceLine> newLineItems = [.. updateDto.LineItems.Select(li => new InvoiceLine
            {
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                TaxRate = li.TaxRate
            })];

            invoice.LineItems = newLineItems;

            // Recalculate totals
            invoice.SubTotal = newLineItems.Sum(li => li.Amount);
            invoice.TaxAmount = newLineItems.Sum(li => li.TaxAmount);
            invoice.TotalAmount = newLineItems.Sum(li => li.TotalAmount);

            Invoice updated = await invoiceRepository.UpdateAsync(invoice);
            return MapToDto(updated);
        }

        public async Task<bool> DeleteInvoiceAsync(int id)
        {
            return await invoiceRepository.DeleteAsync(id);
        }

        public async Task<InvoiceDto> UpdateInvoiceStatusAsync(int id, InvoiceStatus status)
        {
            Invoice invoice = await invoiceRepository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Invoice with ID {id} not found");
            invoice.Status = status;

            Invoice updated = await invoiceRepository.UpdateAsync(invoice);
            return MapToDto(updated);
        }

        public async Task<InvoiceSummaryDto> GetInvoiceSummaryAsync()
        {
            (int total, int paid, int unpaid, int overdue) = await invoiceRepository.GetInvoiceCountsAsync();
            (decimal totalAmount, decimal totalPaid, decimal totalDue) = await invoiceRepository.GetInvoiceTotalsAsync();

            return new InvoiceSummaryDto
            {
                TotalInvoices = total,
                PaidInvoices = paid,
                UnpaidInvoices = unpaid,
                OverdueInvoices = overdue,
                TotalAmount = totalAmount,
                TotalPaid = totalPaid,
                TotalDue = totalDue
            };
        }

        private static InvoiceDto MapToDto(Invoice invoice)
        {
            return new()
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                CompanyId = invoice.CompanyId,
                CompanyName = invoice.Company?.Name ?? string.Empty,
                CompanyEmail = invoice.Company?.Email ?? string.Empty,
                Type = invoice.Type,
                Status = invoice.Status,
                IssueDate = invoice.IssueDate,
                DueDate = invoice.DueDate,
                SubTotal = invoice.SubTotal,
                TaxAmount = invoice.TaxAmount,
                TotalAmount = invoice.TotalAmount,
                AmountPaid = invoice.AmountPaid,
                AmountDue = invoice.AmountDue,
                Notes = invoice.Notes,
                CreatedAt = invoice.CreatedAt,
                LineItems = [.. invoice.LineItems.Select(li => new InvoiceLineDto
                {
                    Id = li.Id,
                    Description = li.Description,
                    Quantity = li.Quantity,
                    UnitPrice = li.UnitPrice,
                    TaxRate = li.TaxRate,
                    Amount = li.Amount,
                    TaxAmount = li.TaxAmount,
                    TotalAmount = li.TotalAmount
                })]
            };
        }
    }
}
