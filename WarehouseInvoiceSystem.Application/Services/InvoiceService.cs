namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.DTOs.Invoice;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Interfaces;

    public class InvoiceService(IInvoiceRepository invoiceRepository,
                                ICompanyRepository companyRepository,
                                IWarehouseRepository warehouseRepository,
                                IProductRepository productRepository,
                                IInventoryService inventoryService,
                                ILocalizationService localizationService) : IInvoiceService
    {
        public async Task<IEnumerable<InvoiceDto>> GetAllInvoicesAsync()
        {
            IEnumerable<Invoice> invoices = await invoiceRepository.GetAllAsync();
            return invoices.Select(MapToDto);
        }

        public async Task<IEnumerable<InvoiceDto>> GetInvoicesByCompanyAsync(Guid companyId)
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

        public async Task<InvoiceDto?> GetInvoiceByIdAsync(Guid id)
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

            // Validate provided warehouse (required)
            if (!await warehouseRepository.ExistsAsync(createDto.WarehouseId))
                throw new KeyNotFoundException($"Warehouse with ID {createDto.WarehouseId} not found");

            // Validate products
            foreach (Guid productId in createDto.LineItems.Select(li => li.ProductId))
            {
                if (!await productRepository.ExistsAsync(productId))
                    throw new KeyNotFoundException($"Product with ID {productId} not found");
            }

            // Generate invoice number
            string invoiceNumber = await invoiceRepository.GenerateInvoiceNumberAsync(createDto.Type);

            // Calculate totals
            List<InvoiceLine> lineItems = createDto.LineItems.Select(li => new InvoiceLine
            {
                ProductId = li.ProductId,
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                TaxRate = li.TaxRate,
                CreatedAt = DateTime.UtcNow,
            }).ToList();

            decimal subTotal = lineItems.Sum(li => li.Amount);
            decimal taxAmount = lineItems.Sum(li => li.TaxAmount);
            decimal totalAmount = lineItems.Sum(li => li.TotalAmount);

            Invoice invoice = new()
            {
                InvoiceNumber = invoiceNumber,
                CompanyId = createDto.CompanyId,
                WarehouseId = createDto.WarehouseId,
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

            // If invoice has products, create inbound/outbound transactions
            if (created.Status == InvoiceStatus.Sent || created.Status == InvoiceStatus.Paid)
            {
                InventoryTransactionType transactionType = created.Type == InvoiceType.Receivable ? InventoryTransactionType.Outbound : InventoryTransactionType.Inbound;
                await CreateInventoryTransactionsForInvoiceAsync(created, createDto.WarehouseId, transactionType);
            }

            return MapToDto(created);
        }

        public async Task<InvoiceDto> UpdateInvoiceAsync(Guid id, UpdateInvoiceDto updateDto)
        {
            Invoice? invoice = await invoiceRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Invoice with ID {id} not found");

            // Validate company exists
            if (!await companyRepository.ExistsAsync(updateDto.CompanyId))
                throw new KeyNotFoundException($"Company with ID {updateDto.CompanyId} not found");

            // Validate warehouse exists
            if (!await warehouseRepository.ExistsAsync(updateDto.WarehouseId))
                throw new KeyNotFoundException($"Warehouse with ID {updateDto.WarehouseId} not found");

            // Track status change for inventory transactions
            InvoiceStatus oldStatus = invoice.Status;
            InvoiceStatus newStatus = updateDto.Status;
            bool shouldCreateTransactions = false;

            // Determine if we need to create inventory transactions
            if (oldStatus == InvoiceStatus.Draft && (newStatus == InvoiceStatus.Sent || newStatus == InvoiceStatus.Paid))
            {
                shouldCreateTransactions = true;
            }

            // Update basic properties
            invoice.CompanyId = updateDto.CompanyId;
            invoice.WarehouseId = updateDto.WarehouseId;
            invoice.Type = updateDto.Type;
            invoice.Status = updateDto.Status;
            invoice.IssueDate = updateDto.IssueDate;
            invoice.DueDate = updateDto.DueDate;
            invoice.Notes = updateDto.Notes;

            // Remove old items
            invoice.LineItems.Clear();

            // Add new/updated items
            List<InvoiceLine> newLineItems = [.. updateDto.LineItems.Select(li => new InvoiceLine
            {
                ProductId = li.ProductId,
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

            // Create inventory transactions if status changed to Sent/Paid
            if (shouldCreateTransactions)
            {
                InventoryTransactionType transactionType = updated.Type == InvoiceType.Receivable ? InventoryTransactionType.Outbound : InventoryTransactionType.Inbound;
                await CreateInventoryTransactionsForInvoiceAsync(updated, updateDto.WarehouseId, transactionType);
            }

            return MapToDto(updated);
        }

        public async Task<bool> DeleteInvoiceAsync(Guid id)
        {
            return await invoiceRepository.DeleteAsync(id);
        }

        public async Task<InvoiceDto> UpdateInvoiceStatusAsync(Guid id, InvoiceStatus status)
        {
            Invoice invoice = await invoiceRepository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Invoice with ID {id} not found");
            invoice.Status = status;

            Invoice updated = await invoiceRepository.UpdateAsync(invoice);
            return MapToDto(updated);
        }

        public async Task<InvoiceSummaryDto> GetPayableInvoiceSummaryAsync()
        {
            (int total, int paid, int unpaid, int overdue) = await invoiceRepository.GetPayableInvoiceCountsAsync();
            (decimal totalAmount, decimal totalPaid, decimal totalDue) = await invoiceRepository.GetPayableInvoiceTotalsAsync();

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

        private async Task CreateInventoryTransactionsForInvoiceAsync(Invoice invoice, Guid warehouseId, InventoryTransactionType transactionType)
        {
            // Create outbound transaction for each line item that has a product
            foreach (InvoiceLine line in invoice.LineItems)
            {
                await inventoryService.CreateTransactionAsync(new DTOs.InventoryTransaction.CreateInventoryTransactionDto
                {
                    ProductId = line.ProductId,
                    WarehouseId = warehouseId,
                    Type = transactionType,
                    Quantity = line.Quantity,
                    SourceDocumentId = invoice.Id,
                    SourceDocumentType = "Invoice",
                    Note = $"{localizationService.GetString("SaleTo")} {invoice.Company.Name} - {invoice.InvoiceNumber}"
                });
            }
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
                WarehouseId = invoice.WarehouseId,
                WarehouseName = invoice.Warehouse?.Name ?? string.Empty,
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
                    ProductId = li.ProductId,
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
