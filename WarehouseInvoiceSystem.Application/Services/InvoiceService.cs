namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.DTOs.InventoryTransaction;
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

        public async Task<IEnumerable<InvoiceDto>> GetAllInvoicesAsync(CancellationToken ct = default)
        {
            IEnumerable<Invoice> invoices = await invoiceRepository.GetAllAsync(ct);
            return invoices.Select(MapToDto);
        }

        public async Task<PagedResult<InvoiceDto>> GetPagedAsync(GetInvoicesQuery query, CancellationToken ct = default)
        {
            PagedResult<Invoice> result = await invoiceRepository.GetPagedAsync(query, ct);
            return new PagedResult<InvoiceDto>
            {
                Items = [.. result.Items.Select(MapToDto)],
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<IEnumerable<InvoiceDto>> GetAllFilteredAsync(GetInvoicesQuery query, CancellationToken ct = default)
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
            PagedResult<Invoice> result = await invoiceRepository.GetPagedAsync(exportQuery, ct);
            return result.Items.Select(MapToDto);
        }

        public async Task<IEnumerable<InvoiceDto>> GetInvoicesByCompanyAsync(Guid companyId, CancellationToken ct = default)
        {
            IEnumerable<Invoice> invoices = await invoiceRepository.GetByCompanyIdAsync(companyId, ct);
            return invoices.Select(MapToDto);
        }

        public async Task<IEnumerable<InvoiceDto>> GetInvoicesByTypeAsync(InvoiceType type, CancellationToken ct = default)
        {
            IEnumerable<Invoice> invoices = await invoiceRepository.GetByTypeAsync(type, ct);
            return invoices.Select(MapToDto);
        }

        public async Task<IEnumerable<InvoiceDto>> GetInvoicesByStatusAsync(InvoiceStatus status, CancellationToken ct = default)
        {
            IEnumerable<Invoice> invoices = await invoiceRepository.GetByStatusAsync(status, ct);
            return invoices.Select(MapToDto);
        }

        public async Task<IEnumerable<InvoiceDto>> GetOverdueInvoicesAsync(CancellationToken ct = default)
        {
            IEnumerable<Invoice> invoices = await invoiceRepository.GetOverdueInvoicesAsync(ct);
            return invoices.Select(MapToDto);
        }

        public async Task<InvoiceDto?> GetInvoiceByIdAsync(Guid id, CancellationToken ct = default)
        {
            Invoice? invoice = await invoiceRepository.GetByIdAsync(id, ct);
            return invoice == null ? null : MapToDto(invoice);
        }

        public async Task<InvoiceDto?> GetInvoiceByNumberAsync(string invoiceNumber, CancellationToken ct = default)
        {
            Invoice? invoice = await invoiceRepository.GetByInvoiceNumberAsync(invoiceNumber, ct);
            return invoice == null ? null : MapToDto(invoice);
        }

        // ── Create ────────────────────────────────────────────────────────────────────

        public async Task<Guid> CreateInvoiceAsync(CreateInvoiceDto createDto)
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

            Guid createdId = await invoiceRepository.CreateAsync(invoice);
            return createdId;
        }

        // ── Update ────────────────────────────────────────────────────────────────────

        public async Task UpdateInvoiceAsync(Guid id, UpdateInvoiceDto updateDto)
        {
            Invoice? invoice = await invoiceRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Invoice with ID {id} not found");

            if (invoice.Status != InvoiceStatus.Draft)
                throw new InvalidOperationException(
                    $"Invoice {invoice.InvoiceNumber} cannot be edited (current: {invoice.Status}). Only Draft invoices can be edited.");

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

            MergeLineItems(invoice, updateDto.LineItems);

            await invoiceRepository.UpdateAsync(invoice);
        }

        // ── 3-way line item merge ─────────────────────────────────────────────────────
        // Update existing lines, remove deleted ones, insert new ones
        private static void MergeLineItems(Invoice invoice, IEnumerable<UpdateInvoiceLineDto> incoming)
        {
            DateTime now = DateTime.UtcNow;
            HashSet<Guid> incomingIds = incoming
                .Where(li => li.Id != Guid.Empty)
                .Select(li => li.Id)
                .ToHashSet();

            // Lines absent from the incoming DTO were removed by the user — soft-delete them
            foreach (InvoiceLine existing in invoice.LineItems.Where(li => !incomingIds.Contains(li.Id)))
                existing.DeletedOn = now;

            // Update existing or insert new
            Dictionary<Guid, InvoiceLine> existingById = invoice.LineItems.ToDictionary(li => li.Id);
            List<InvoiceLine> newLines = [];

            foreach (UpdateInvoiceLineDto li in incoming)
            {
                if (li.Id != Guid.Empty && existingById.TryGetValue(li.Id, out InvoiceLine? existing))
                {
                    existing.ProductId = li.ProductId;
                    existing.Description = li.Description;
                    existing.Quantity = li.Quantity;
                    existing.UnitPrice = li.UnitPrice;
                    existing.TaxRate = li.TaxRate;
                }
                else
                {
                    newLines.Add(new InvoiceLine
                    {
                        ProductId = li.ProductId,
                        Description = li.Description,
                        Quantity = li.Quantity,
                        UnitPrice = li.UnitPrice,
                        TaxRate = li.TaxRate
                    });
                }
            }

            foreach (InvoiceLine line in newLines)
                invoice.LineItems.Add(line);

            // Recalculate totals from active lines only
            List<InvoiceLine> activeLines = invoice.LineItems
                .Where(li => li.DeletedOn == null)
                .ToList();

            invoice.SubTotal = activeLines.Sum(li => li.Amount);
            invoice.TaxAmount = activeLines.Sum(li => li.TaxAmount);
            invoice.TotalAmount = activeLines.Sum(li => li.TotalAmount);
        }

        public async Task UpdateNotesAsync(Guid id, string? notes, CancellationToken ct = default)
        {
            Invoice? invoice = await invoiceRepository.GetByIdAsync(id, ct)
                ?? throw new KeyNotFoundException($"Invoice with ID {id} not found");

            invoice.Notes = notes;
            await invoiceRepository.UpdateAsync(invoice);
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

            try
            {
                await CreateInventoryTransactionsIfNeededAsync(updated);
            }
            catch (Exception)
            {
                // Inventory creation failed — roll back the status change so the invoice
                // doesn't appear as Sent with no stock movement recorded.
                invoice.Status = InvoiceStatus.Draft;
                await invoiceRepository.UpdateAsync(invoice);
                throw;
            }

            return MapToDto(updated);
        }

        /// <summary>Sent/Overdue → Draft. Reverses inventory transactions. Not allowed for PartiallyPaid/Cancelled/Paid.</summary>
        public async Task<InvoiceDto> RevertToDraftAsync(Guid id)
        {
            Invoice? invoice = await invoiceRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Invoice with ID {id} not found");

            if (invoice.Status != InvoiceStatus.Sent && invoice.Status != InvoiceStatus.Overdue)
                throw new InvalidOperationException(
                    $"Invoice {invoice.InvoiceNumber} cannot be reverted to Draft (current: {invoice.Status}). Only Sent or Overdue invoices can be reverted.");

            InvoiceStatus previousStatus = invoice.Status;
            invoice.Status = InvoiceStatus.Draft;
            Invoice updated = await invoiceRepository.UpdateAsync(invoice);

            try
            {
                await CreateReverseTransactionsIfNeeded(updated);
            }
            catch (Exception)
            {
                invoice.Status = previousStatus;
                await invoiceRepository.UpdateAsync(invoice);
                throw;
            }

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

            InvoiceStatus previousStatus = invoice.Status;
            invoice.Status = InvoiceStatus.Paid;
            Invoice updated = await invoiceRepository.UpdateAsync(invoice);

            try
            {
                // Covers the Payable Draft → Overdue → Paid edge case where stock was never moved.
                // No-op for invoices that already had transactions created (e.g. normal Sent → Paid flow).
                await CreateInventoryTransactionsIfNeededAsync(updated);
            }
            catch (Exception)
            {
                invoice.Status = previousStatus;
                await invoiceRepository.UpdateAsync(invoice);
                throw;
            }

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

            InvoiceStatus previousStatus = invoice.Status;
            invoice.Status = InvoiceStatus.Cancelled;
            Invoice updated = await invoiceRepository.UpdateAsync(invoice);

            try
            {
                // No-op for Draft since no transactions exist yet. Reverses for Sent/Overdue.
                await CreateReverseTransactionsIfNeeded(updated);
            }
            catch (Exception)
            {
                invoice.Status = previousStatus;
                await invoiceRepository.UpdateAsync(invoice);
                throw;
            }

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

            string note = GenerateNote(invoice, type);

            IEnumerable<CreateInventoryTransactionDto> items = invoice.LineItems.Select(line =>
                new DTOs.InventoryTransaction.CreateInventoryTransactionDto
                {
                    ProductId = line.ProductId,
                    WarehouseId = invoice.WarehouseId,
                    Type = type,
                    Quantity = line.Quantity,
                    SourceDocumentId = invoice.Id,
                    SourceDocumentType = invoiceString,
                    Note = note
                });

            await inventoryService.CreateBatchAsync(invoice.WarehouseId, items);
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

        public async Task<InvoiceSummaryDto> GetPayableInvoiceSummaryAsync(CancellationToken ct = default)
        {
            (int total, int paid, int unpaid, int overdue) = await invoiceRepository.GetPayableInvoiceCountsAsync(ct);
            (decimal totalAmount, decimal totalPaid, decimal totalDue) = await invoiceRepository.GetPayableInvoiceTotalsAsync(ct);

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