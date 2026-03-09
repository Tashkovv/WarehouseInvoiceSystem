namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.DTOs.Invoice;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public class InvoiceService(IInvoiceRepository invoiceRepository,
                                ICompanyRepository companyRepository,
                                IWarehouseRepository warehouseRepository,
                                IProductRepository productRepository,
                                IInventoryService inventoryService,
                                ILocalizationService localizationService,
                                IInventoryTransactionRepository transactionRepository) : IInvoiceService
    {
        private const string invoiceString = "Invoice";

        // ── Queries ───────────────────────────────────────────────────────────────────

        public async Task<IEnumerable<InvoiceDto>> GetAllInvoicesAsync()
        {
            IEnumerable<Invoice> invoices = await invoiceRepository.GetAllAsync();
            return invoices.Select(MapToDto);
        }

        public async Task<PagedResult<InvoiceDto>> GetPagedAsync(GetInvoicesQuery query)
        {
            PagedResult<Invoice> result = await invoiceRepository.GetPagedAsync(query);
            return new PagedResult<InvoiceDto>
            {
                Items = [.. result.Items.Select(MapToDto)],
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<IEnumerable<InvoiceDto>> GetAllFilteredAsync(GetInvoicesQuery query)
        {
            GetInvoicesQuery exportQuery = new()
            {
                Page = 1,
                PageSize = int.MaxValue,
                SortBy = query.SortBy,
                SortAscending = query.SortAscending,
                Search = query.Search,
                Status = query.Status,
                Statuses = query.Statuses,
                Type = query.Type,
                CompanyName = query.CompanyName,
                AmountMin = query.AmountMin,
                AmountMax = query.AmountMax,
                IssueDateFrom = query.IssueDateFrom,
                IssueDateTo = query.IssueDateTo,
                DueDateFrom = query.DueDateFrom,
                DueDateTo = query.DueDateTo
            };
            PagedResult<Invoice> result = await invoiceRepository.GetPagedAsync(exportQuery);
            return result.Items.Select(MapToDto);
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

        // ── Create ────────────────────────────────────────────────────────────────────

        public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto createDto)
        {
            if (!await companyRepository.ExistsAsync(createDto.CompanyId))
                throw new KeyNotFoundException($"Company with ID {createDto.CompanyId} not found");

            if (!await warehouseRepository.ExistsAsync(createDto.WarehouseId))
                throw new KeyNotFoundException($"Warehouse with ID {createDto.WarehouseId} not found");

            var productIds = createDto.LineItems.Select(li => li.ProductId).ToList();
            if (!await productRepository.AllExistAsync(productIds))
                throw new KeyNotFoundException("One or more products in the line items were not found");

            string invoiceNumber = await invoiceRepository.GenerateInvoiceNumberAsync(createDto.Type);

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
            return MapToDto(created);
        }

        // ── Update ────────────────────────────────────────────────────────────────────

        public async Task<InvoiceDto> UpdateInvoiceAsync(Guid id, UpdateInvoiceDto updateDto)
        {
            Invoice? invoice = await invoiceRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Invoice with ID {id} not found");

            if (!await companyRepository.ExistsAsync(updateDto.CompanyId))
                throw new KeyNotFoundException($"Company with ID {updateDto.CompanyId} not found");

            if (!await warehouseRepository.ExistsAsync(updateDto.WarehouseId))
                throw new KeyNotFoundException($"Warehouse with ID {updateDto.WarehouseId} not found");

            var productIds = updateDto.LineItems.Select(li => li.ProductId).ToList();
            if (!await productRepository.AllExistAsync(productIds))
                throw new KeyNotFoundException("One or more products in the line items were not found");

            invoice.CompanyId = updateDto.CompanyId;
            invoice.WarehouseId = updateDto.WarehouseId;
            invoice.Type = updateDto.Type;
            invoice.IssueDate = updateDto.IssueDate;
            invoice.DueDate = updateDto.DueDate;
            invoice.Notes = updateDto.Notes;

            invoice.LineItems.Clear();
            List<InvoiceLine> newLineItems = [.. updateDto.LineItems.Select(li => new InvoiceLine
            {
                ProductId = li.ProductId,
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                TaxRate = li.TaxRate
            })];

            invoice.LineItems = newLineItems;
            invoice.SubTotal = newLineItems.Sum(li => li.Amount);
            invoice.TaxAmount = newLineItems.Sum(li => li.TaxAmount);
            invoice.TotalAmount = newLineItems.Sum(li => li.TotalAmount);

            Invoice updated = await invoiceRepository.UpdateAsync(invoice);
            return MapToDto(updated);
        }

        // ── Delete ────────────────────────────────────────────────────────────────────

        public async Task<bool> DeleteInvoiceAsync(Guid id)
        {
            Invoice? invoice = await invoiceRepository.GetByIdAsync(id);
            if (invoice == null) return false;

            if (invoice.Status != InvoiceStatus.Draft && invoice.Status != InvoiceStatus.Cancelled)
                await CreateReverseTransactionsIfNeeded(invoice);

            return await invoiceRepository.DeleteAsync(id);
        }

        // ── Status transitions ────────────────────────────────────────────────────────

        /// <summary>Draft → Sent. Receivable only. Creates outbound inventory transactions.</summary>
        public async Task<InvoiceDto> SendAsync(Guid id)
        {
            Invoice? invoice = await invoiceRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Invoice with ID {id} not found");

            if (invoice.Type != InvoiceType.Receivable)
                throw new InvalidOperationException(
                    $"Invoice {invoice.InvoiceNumber} is Payable and cannot be sent.");

            if (invoice.Status != InvoiceStatus.Draft)
                throw new InvalidOperationException(
                    $"Invoice {invoice.InvoiceNumber} must be in Draft to send (current: {invoice.Status}).");

            invoice.Status = InvoiceStatus.Sent;
            Invoice updated = await invoiceRepository.UpdateAsync(invoice);
            await CreateInventoryTransactionsIfNeededAsync(updated);

            return MapToDto(updated);
        }

        /// <summary>
        /// Sent/PartiallyPaid/Overdue → Paid.
        /// Calls CreateInventoryTransactionsIfNeededAsync to handle the edge case where
        /// a Payable invoice was never sent (went Draft → Overdue) and stock was never moved.
        /// The idempotency guard inside ensures no duplicate transactions for normal flows.
        /// </summary>
        public async Task<InvoiceDto> MarkAsPaidAsync(Guid id)
        {
            Invoice? invoice = await invoiceRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Invoice with ID {id} not found");

            if (invoice.Status != InvoiceStatus.Sent &&
                invoice.Status != InvoiceStatus.PartiallyPaid &&
                invoice.Status != InvoiceStatus.Overdue)
                throw new InvalidOperationException(
                    $"Invoice {invoice.InvoiceNumber} cannot be marked as paid (current: {invoice.Status}).");

            invoice.Status = InvoiceStatus.Paid;
            Invoice updated = await invoiceRepository.UpdateAsync(invoice);

            // Covers the Payable Draft → Overdue → Paid edge case where stock was never moved.
            // No-op for invoices that already had transactions created (e.g. normal Sent → Paid flow).
            await CreateInventoryTransactionsIfNeededAsync(updated);

            return MapToDto(updated);
        }

        /// <summary>
        /// Draft/Sent/Overdue → Cancelled. Reverses inventory transactions if any exist.
        /// Blocked for PartiallyPaid and Paid.
        /// </summary>
        public async Task<InvoiceDto> CancelAsync(Guid id)
        {
            Invoice? invoice = await invoiceRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Invoice with ID {id} not found");

            if (invoice.Status == InvoiceStatus.PartiallyPaid)
                throw new InvalidOperationException(
                    $"Invoice {invoice.InvoiceNumber} has partial payments and cannot be cancelled.");

            if (invoice.Status == InvoiceStatus.Paid)
                throw new InvalidOperationException(
                    $"Invoice {invoice.InvoiceNumber} is fully paid and cannot be cancelled.");

            if (invoice.Status == InvoiceStatus.Cancelled)
                throw new InvalidOperationException(
                    $"Invoice {invoice.InvoiceNumber} is already cancelled.");

            invoice.Status = InvoiceStatus.Cancelled;
            Invoice updated = await invoiceRepository.UpdateAsync(invoice);

            // No-op for Draft since no transactions exist yet. Reverses for Sent/Overdue.
            await CreateReverseTransactionsIfNeeded(updated);

            return MapToDto(updated);
        }

        /// <summary>
        /// Any non-terminal status → Overdue. Called exclusively by BackgroundJobService.
        /// Pure status flag — no inventory changes. Overdue is a financial/time state only.
        /// </summary>
        public async Task<InvoiceDto> MarkAsOverdueAsync(Guid id)
        {
            Invoice? invoice = await invoiceRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Invoice with ID {id} not found");

            invoice.Status = InvoiceStatus.Overdue;
            Invoice updated = await invoiceRepository.UpdateAsync(invoice);
            return MapToDto(updated);
        }

        // ── Inventory helpers ─────────────────────────────────────────────────────────

        public async Task CreateInventoryTransactionsIfNeededAsync(Invoice invoice)
        {
            bool alreadyCreated = await transactionRepository
                .HasTransactionsForDocumentAsync(invoice.Id, invoiceString);

            if (alreadyCreated) return;

            InventoryTransactionType type = invoice.Type == InvoiceType.Receivable
                ? InventoryTransactionType.Outbound
                : InventoryTransactionType.Inbound;

            foreach (InvoiceLine line in invoice.LineItems)
            {
                await inventoryService.CreateTransactionAsync(
                    new DTOs.InventoryTransaction.CreateInventoryTransactionDto
                    {
                        ProductId = line.ProductId,
                        WarehouseId = invoice.WarehouseId,
                        Type = type,
                        Quantity = line.Quantity,
                        SourceDocumentId = invoice.Id,
                        SourceDocumentType = invoiceString,
                        Note = GenerateNote(invoice, type)
                    });
            }
        }

        public async Task CreateReverseTransactionsIfNeeded(Invoice invoice, string? reason = null)
        {
            bool alreadyCreated = await transactionRepository
                .HasTransactionsForDocumentAsync(invoice.Id, $"{invoiceString}_Reversal");

            if (alreadyCreated) return;

            await inventoryService.ReverseTransactionsForDocumentAsync(
                    invoice.Id,
                    invoiceString,
                    reason ?? $"{localizationService.GetString("InvoiceCancelled")}: {invoice.InvoiceNumber}");
        }

        // ── Summary ───────────────────────────────────────────────────────────────────

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

        // ── Helpers ───────────────────────────────────────────────────────────────────

        private string GenerateNote(Invoice invoice, InventoryTransactionType transactionType)
        {
            if (transactionType == InventoryTransactionType.Inbound)
                return $"{localizationService.GetString("PurchaseFrom")} {invoice.Company.Name} - {invoice.InvoiceNumber}";
            else if (transactionType == InventoryTransactionType.Outbound)
                return $"{localizationService.GetString("SaleTo")} {invoice.Company.Name} - {invoice.InvoiceNumber}";
            else
                return $"{localizationService.GetString("InventoryTransactionForInvoice")}: {invoice.InvoiceNumber}";
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
                LineItems = invoice.LineItems == null ? [] : [.. invoice.LineItems.Select(li => new InvoiceLineDto
                {
                    Id = li.Id,
                    ProductId = li.ProductId,
                    Description = li.Product.Description ?? string.Empty,
                    ProductCode = li.Product.Code,
                    ProductName = li.Product.Name,
                    ProductUnit = li.Product.Unit,
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