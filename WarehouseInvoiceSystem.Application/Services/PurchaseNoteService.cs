namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.DTOs.PurchaseNote;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public class PurchaseNoteService(
        IPurchaseNoteRepository purchaseNoteRepository,
        IIndividualRepository individualRepository,
        IWarehouseRepository warehouseRepository,
        IProductRepository productRepository,
        IInventoryService inventoryService,
        ILocalizationService localizationService,
        IInventoryTransactionRepository transactionRepository) : IPurchaseNoteService
    {
        private const string purchaseNoteString = "PurchaseNote";

        public async Task<IEnumerable<PurchaseNoteDto>> GetAllPurchaseNotesAsync()
        {
            IEnumerable<PurchaseNote> notes = await purchaseNoteRepository.GetAllAsync();
            return notes.Select(MapToDto);
        }

        public async Task<PagedResult<PurchaseNoteDto>> GetPagedAsync(GetPurchaseNotesQuery query)
        {
            PagedResult<PurchaseNote> result = await purchaseNoteRepository.GetPagedAsync(query);
            return new PagedResult<PurchaseNoteDto>
            {
                Items = [.. result.Items.Select(MapToDto)],
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<IEnumerable<PurchaseNoteDto>> GetAllFilteredAsync(GetPurchaseNotesQuery query)
        {
            GetPurchaseNotesQuery exportQuery = new()
            {
                Page = 1,
                PageSize = int.MaxValue,
                SortBy = query.SortBy,
                SortAscending = query.SortAscending,
                Search = query.Search,
                Status = query.Status,
                IndividualName = query.IndividualName,
                AmountMin = query.AmountMin,
                AmountMax = query.AmountMax,
                DateFrom = query.DateFrom,
                DateTo = query.DateTo
            };
            PagedResult<PurchaseNote> result = await purchaseNoteRepository.GetPagedAsync(exportQuery);
            return result.Items.Select(MapToDto);
        }

        public async Task<PurchaseNoteDto?> GetPurchaseNoteByIdAsync(Guid id)
        {
            PurchaseNote? note = await purchaseNoteRepository.GetByIdAsync(id);
            return note == null ? null : MapToDto(note);
        }

        public async Task<PurchaseNoteDto?> GetPurchaseNoteByNumberAsync(string noteNumber)
        {
            PurchaseNote? note = await purchaseNoteRepository.GetByNoteNumberAsync(noteNumber);
            return note == null ? null : MapToDto(note);
        }

        public async Task<IEnumerable<PurchaseNoteDto>> GetPurchaseNotesByIndividualAsync(Guid individualId)
        {
            IEnumerable<PurchaseNote> notes = await purchaseNoteRepository.GetByIndividualIdAsync(individualId);
            return notes.Select(MapToDto);
        }

        public async Task<IEnumerable<PurchaseNoteDto>> GetPurchaseNotesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            IEnumerable<PurchaseNote> notes = await purchaseNoteRepository.GetByDateRangeAsync(startDate, endDate);
            return notes.Select(MapToDto);
        }

        public async Task<IEnumerable<PurchaseNoteDto>> GetPurchaseNotesByStatusAsync(PurchaseNoteStatus status)
        {
            IEnumerable<PurchaseNote> notes = await purchaseNoteRepository.GetByStatusAsync(status);
            return notes.Select(MapToDto);
        }

        public async Task<PurchaseNoteDto> CreatePurchaseNoteAsync(CreatePurchaseNoteDto createDto)
        {
            // Validate individual exists
            if (!await individualRepository.ExistsAsync(createDto.IndividualId))
                throw new KeyNotFoundException($"Individual with ID {createDto.IndividualId} not found");

            // Validate warehouse (required)
            Guid warehouseId = createDto.WarehouseId;
            if (!await warehouseRepository.ExistsAsync(warehouseId))
                throw new KeyNotFoundException($"Warehouse with ID {warehouseId} not found");

            // Validate products
            var productIds = createDto.LineItems.Select(li => li.ProductId).ToList();
            if (!await productRepository.AllExistAsync(productIds))
                throw new KeyNotFoundException("One or more products in the line items were not found");

            // Generate note number
            string noteNumber = await purchaseNoteRepository.GenerateNoteNumberAsync();

            // Create line items
            List<PurchaseNoteLine> lineItems = createDto.LineItems.Select(li => new PurchaseNoteLine
            {
                ProductId = li.ProductId,
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                CreatedAt = DateTime.UtcNow,
            }).ToList();

            // Calculate totals
            decimal subTotal = lineItems.Sum(li => li.Amount);

            DateTime? itemPaidDate;
            if (createDto.PurchaseDate > DateTime.UtcNow)
            {
                itemPaidDate = createDto.MarkAsPaid
                             ? createDto.PurchaseDate
                             : null;
            }
            else
            {
                itemPaidDate = createDto.MarkAsPaid
                             ? DateTime.UtcNow
                             : null;
            }

            // Create purchase note
            PurchaseNote purchaseNote = new()
            {
                NoteNumber = noteNumber,
                IndividualId = createDto.IndividualId,
                WarehouseId = warehouseId,
                PurchaseDate = createDto.PurchaseDate,
                SubTotal = subTotal,
                TotalAmount = subTotal,
                Status = createDto.MarkAsPaid ? PurchaseNoteStatus.Paid : PurchaseNoteStatus.Completed,
                PaidDate = itemPaidDate,
                Notes = createDto.Notes,
                LineItems = lineItems
            };

            PurchaseNote created = await purchaseNoteRepository.CreateAsync(purchaseNote);

            // Goods are received at creation time — always create inbound transactions.
            await CreateInventoryTransactionsAsync(created);

            return MapToDto(created);
        }

        public async Task<PurchaseNoteDto> UpdatePurchaseNoteAsync(Guid id, UpdatePurchaseNoteDto updateDto)
        {
            PurchaseNote? purchaseNote = await purchaseNoteRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Purchase note with ID {id} not found");

            // Validate individual exists
            if (!await individualRepository.ExistsAsync(updateDto.IndividualId))
                throw new KeyNotFoundException($"Individual with ID {updateDto.IndividualId} not found");

            // Validate warehouse
            if (!await warehouseRepository.ExistsAsync(updateDto.WarehouseId))
                throw new KeyNotFoundException($"Warehouse with ID {updateDto.WarehouseId} not found");

            // Validate products
            var productIds = updateDto.LineItems.Select(li => li.ProductId).ToList();
            if (!await productRepository.AllExistAsync(productIds))
                throw new KeyNotFoundException("One or more products in the line items were not found");

            // Track status change for inventory transactions
            PurchaseNoteStatus oldStatus = purchaseNote.Status;
            PurchaseNoteStatus newStatus = updateDto.Status;

            // Update properties
            purchaseNote.IndividualId = updateDto.IndividualId;
            purchaseNote.WarehouseId = updateDto.WarehouseId;
            purchaseNote.PurchaseDate = updateDto.PurchaseDate;
            purchaseNote.Status = updateDto.Status;
            purchaseNote.PaidDate = updateDto.PaidDate;
            purchaseNote.Notes = updateDto.Notes;

            // Update line items
            purchaseNote.LineItems.Clear();
            foreach (UpdatePurchaseNoteLineDto lineDto in updateDto.LineItems)
            {
                if (!await productRepository.ExistsAsync(lineDto.ProductId))
                    throw new KeyNotFoundException($"Product with ID {lineDto.ProductId} not found");

                purchaseNote.LineItems.Add(new PurchaseNoteLine
                {
                    ProductId = lineDto.ProductId,
                    Description = lineDto.Description,
                    Quantity = lineDto.Quantity,
                    UnitPrice = lineDto.UnitPrice
                });
            }

            // Recalculate totals
            purchaseNote.SubTotal = purchaseNote.LineItems.Sum(li => li.Amount);
            purchaseNote.TotalAmount = purchaseNote.SubTotal;

            PurchaseNote updated = await purchaseNoteRepository.UpdateAsync(purchaseNote);

            // ── Inventory logic ──────────────────────────────────────────────────────
            //
            // Only create transactions if moving out of Draft for the first time.
            // HasTransactionsForDocumentAsync guards against duplicates.
            //
            // Completed → Paid: purely financial, no stock movement.
            // Completed → Completed or Paid → Paid: no change needed.
            if (oldStatus == PurchaseNoteStatus.Draft &&
               (newStatus == PurchaseNoteStatus.Completed || newStatus == PurchaseNoteStatus.Paid))
            {
                bool alreadyCreated = await transactionRepository
                    .HasTransactionsForDocumentAsync(updated.Id, purchaseNoteString);

                if (!alreadyCreated)
                    await CreateInventoryTransactionsAsync(updated);
            }

            return MapToDto(updated);
        }

        public async Task<bool> DeletePurchaseNoteAsync(Guid id)
        {
            PurchaseNote? note = await purchaseNoteRepository.GetByIdAsync(id);
            if (note == null)
                return false;

            bool hasTransactions = await transactionRepository
                .HasTransactionsForDocumentAsync(id, purchaseNoteString);

            if (hasTransactions)
            {
                // Reverse the inbound stock movements — goods are being "un-received"
                await inventoryService.ReverseTransactionsForDocumentAsync(
                    id,
                    purchaseNoteString,
                    $"{localizationService.GetString("PurchaseNoteDeleted")}: {note.NoteNumber}");
            }

            return await purchaseNoteRepository.DeleteAsync(id);
        }

        // ── MarkAsPaid ────────────────────────────────────────────────────────────────
        //
        // Marks a Completed purchase note as Paid. This is a purely financial status
        // change — the goods were already received and accounted for in stock when the
        // note was first created (Completed). No new inventory transaction is needed.

        public async Task<PurchaseNoteDto> MarkAsPaidAsync(Guid id)
        {
            PurchaseNote? purchaseNote = await purchaseNoteRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Purchase note with ID {id} not found");

            // Guard: only Completed notes can be marked as paid this way.
            // Draft notes should go through the full update flow.
            if (purchaseNote.Status == PurchaseNoteStatus.Draft)
                throw new InvalidOperationException(
                    $"Purchase note {purchaseNote.NoteNumber} is still a Draft. " +
                    "Complete it before marking as paid.");

            if (purchaseNote.Status == PurchaseNoteStatus.Paid)
                throw new InvalidOperationException(
                    $"Purchase note {purchaseNote.NoteNumber} is already paid.");

            purchaseNote.Status = PurchaseNoteStatus.Paid;
            purchaseNote.PaidDate = DateTime.UtcNow;

            PurchaseNote updated = await purchaseNoteRepository.UpdateAsync(purchaseNote);

            // No inventory transaction — this is financial settlement only.
            // Stock was adjusted when the note was created as Completed.

            return MapToDto(updated);
        }

        private async Task CreateInventoryTransactionsAsync(PurchaseNote purchaseNote)
        {
            // Create inbound transaction for each line item
            foreach (PurchaseNoteLine line in purchaseNote.LineItems)
            {
                await inventoryService.CreateTransactionAsync(new DTOs.InventoryTransaction.CreateInventoryTransactionDto
                {
                    ProductId = line.ProductId,
                    WarehouseId = purchaseNote.WarehouseId,
                    Type = InventoryTransactionType.Inbound,
                    Quantity = line.Quantity,
                    SourceDocumentId = purchaseNote.Id,
                    SourceDocumentType = purchaseNoteString,
                    Note = $"{localizationService.GetString("PurchaseFrom")} {purchaseNote.Individual.FullName} - {purchaseNote.NoteNumber}"
                });
            }
        }

        private static PurchaseNoteDto MapToDto(PurchaseNote note)
        {
            return new PurchaseNoteDto
            {
                Id = note.Id,
                NoteNumber = note.NoteNumber,
                IndividualId = note.IndividualId,
                IndividualFullName = note.Individual?.FullName ?? string.Empty,
                IndividualIdentificationNumber = note.Individual?.IdentificationNumber ?? string.Empty,
                WarehouseId = note.WarehouseId,
                WarehouseName = note.Warehouse?.Name,
                PurchaseDate = note.PurchaseDate,
                SubTotal = note.SubTotal,
                TotalAmount = note.TotalAmount,
                Status = note.Status,
                PaidDate = note.PaidDate,
                Notes = note.Notes,
                CreatedAt = note.CreatedAt,
                LineItems = [.. note.LineItems.Select(li => new PurchaseNoteLineDto
                {
                    Id = li.Id,
                    PurchaseNoteId = li.PurchaseNoteId,
                    ProductId = li.ProductId,
                    ProductCode = li.Product.Code,
                    ProductName = li.Product.Name,
                    ProductUnit = li.Product.Unit,
                    Description = li.Product.Description ?? string.Empty,
                    Quantity = li.Quantity,
                    UnitPrice = li.UnitPrice,
                    Amount = li.Amount
                })]
            };
        }
    }
}