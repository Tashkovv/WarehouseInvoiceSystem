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

        public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto createDto)
        {
            // Validate company exists
            if (!await companyRepository.ExistsAsync(createDto.CompanyId))
                throw new KeyNotFoundException($"Company with ID {createDto.CompanyId} not found");

            // Validate provided warehouse (required)
            if (!await warehouseRepository.ExistsAsync(createDto.WarehouseId))
                throw new KeyNotFoundException($"Warehouse with ID {createDto.WarehouseId} not found");

            // Validate products
            var productIds = createDto.LineItems.Select(li => li.ProductId).ToList();
            if (!await productRepository.AllExistAsync(productIds))
                throw new KeyNotFoundException("One or more products in the line items were not found");

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

            // Validate products
            var productIds = updateDto.LineItems.Select(li => li.ProductId).ToList();
            if (!await productRepository.AllExistAsync(productIds))
                throw new KeyNotFoundException("One or more products in the line items were not found");

            // Track status change for inventory transactions
            InvoiceStatus oldStatus = invoice.Status;
            InvoiceStatus newStatus = updateDto.Status;

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

            if (newStatus == InvoiceStatus.Cancelled && oldStatus != InvoiceStatus.Cancelled)
            {
                await CreateReverseTransactionsIfNeeded(updated);
            }
            else if (oldStatus == InvoiceStatus.Cancelled &&
                     newStatus != InvoiceStatus.Cancelled &&
                     newStatus != InvoiceStatus.Draft)
            {
                // Restore stock by soft-deleting the reversal — original transaction becomes live again
                await inventoryService.SoftDeleteReversalForDocumentAsync(updated.Id, invoiceString);
            }
            else if (oldStatus == InvoiceStatus.Draft &&
                     newStatus != InvoiceStatus.Draft &&
                     newStatus != InvoiceStatus.Cancelled)
            {
                await CreateInventoryTransactionsIfNeededAsync(updated);
            }

            return MapToDto(updated);
        }

        public async Task<bool> DeleteInvoiceAsync(Guid id)
        {
            Invoice? invoice = await invoiceRepository.GetByIdAsync(id);
            if (invoice == null) return false;

            // Reverse any stock movements before soft-deleting
            if (invoice.Status != InvoiceStatus.Draft && invoice.Status != InvoiceStatus.Cancelled)
            {
                await CreateReverseTransactionsIfNeeded(invoice);
            }

            return await invoiceRepository.DeleteAsync(id);
        }

        public async Task<InvoiceDto> UpdateInvoiceStatusAsync(Guid id, InvoiceStatus newStatus)
        {
            Invoice invoice = await invoiceRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Invoice with ID {id} not found");

            InvoiceStatus oldStatus = invoice.Status;
            invoice.Status = newStatus;
            Invoice updated = await invoiceRepository.UpdateAsync(invoice);

            if (newStatus == InvoiceStatus.Cancelled && oldStatus != InvoiceStatus.Cancelled)
            {
                await CreateReverseTransactionsIfNeeded(updated);
            }
            else if (oldStatus == InvoiceStatus.Cancelled &&
                     newStatus != InvoiceStatus.Cancelled &&
                     newStatus != InvoiceStatus.Draft)
            {
                // Restore stock by soft-deleting the reversal — original transaction becomes live again
                await inventoryService.SoftDeleteReversalForDocumentAsync(updated.Id, invoiceString);
            }
            else if (oldStatus == InvoiceStatus.Draft &&
                     newStatus != InvoiceStatus.Draft &&
                     newStatus != InvoiceStatus.Cancelled)
            {
                await CreateInventoryTransactionsIfNeededAsync(updated);
            }

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

        public async Task CreateInventoryTransactionsIfNeededAsync(Invoice invoice)
        {
            // Only create if not already created for this document
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
            // Only create if not already created for this document
            bool alreadyCreated = await transactionRepository
                .HasTransactionsForDocumentAsync(invoice.Id, $"{invoiceString}_Reversal");

            if (alreadyCreated) return;

            await inventoryService.ReverseTransactionsForDocumentAsync(
                    invoice.Id,
                    invoiceString,
                    reason ?? $"{localizationService.GetString("InvoiceCancelled")}: {invoice.InvoiceNumber}");
        }

        private string GenerateNote(Invoice invoice, InventoryTransactionType transactionType)
        {
            if (transactionType == InventoryTransactionType.Inbound)
            {
                return $"{localizationService.GetString("PurchaseFrom")} {invoice.Company.Name} - {invoice.InvoiceNumber}";
            }
            else if (transactionType == InventoryTransactionType.Outbound)
            {
                return $"{localizationService.GetString("SaleTo")} {invoice.Company.Name} - {invoice.InvoiceNumber}";
            }
            else
            {
                return $"{localizationService.GetString("InventoryTransactionForInvoice")}: {invoice.InvoiceNumber}";
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