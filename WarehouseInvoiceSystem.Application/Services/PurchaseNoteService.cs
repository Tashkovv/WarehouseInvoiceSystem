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
        ILocalizationService localizationService) : IPurchaseNoteService
    {
        private const string DocumentType = "PurchaseNote";

        // ── Queries ───────────────────────────────────────────────────────────────────

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

        // ── Create ────────────────────────────────────────────────────────────────────

        public async Task<PurchaseNoteDto> CreatePurchaseNoteAsync(CreatePurchaseNoteDto createDto)
        {
            if (!await individualRepository.ExistsAsync(createDto.IndividualId))
                throw new KeyNotFoundException($"Individual with ID {createDto.IndividualId} not found");

            if (!await warehouseRepository.ExistsAsync(createDto.WarehouseId))
                throw new KeyNotFoundException($"Warehouse with ID {createDto.WarehouseId} not found");

            var productIds = createDto.LineItems.Select(li => li.ProductId).ToList();
            if (!await productRepository.AllExistAsync(productIds))
                throw new KeyNotFoundException("One or more products in the line items were not found");

            string noteNumber = await purchaseNoteRepository.GenerateNoteNumberAsync();

            List<PurchaseNoteLine> lineItems = createDto.LineItems.Select(li => new PurchaseNoteLine
            {
                ProductId = li.ProductId,
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                CreatedAt = DateTime.UtcNow,
            }).ToList();

            decimal subTotal = lineItems.Sum(li => li.Amount);

            // Create always starts as Draft, unless MarkAsPaid is set
            // MarkAsPaid skips Draft and Pending — goods received + paid on the spot
            PurchaseNoteStatus initialStatus = createDto.MarkAsPaid
                ? PurchaseNoteStatus.Paid
                : PurchaseNoteStatus.Draft;

            DateTime? paidDate = createDto.MarkAsPaid ? DateTime.UtcNow : null;

            PurchaseNote purchaseNote = new()
            {
                NoteNumber = noteNumber,
                IndividualId = createDto.IndividualId,
                WarehouseId = createDto.WarehouseId,
                PurchaseDate = createDto.PurchaseDate,
                SubTotal = subTotal,
                TotalAmount = subTotal,
                Status = initialStatus,
                PaidDate = paidDate,
                Notes = createDto.Notes,
                LineItems = lineItems
            };

            PurchaseNote created = await purchaseNoteRepository.CreateAsync(purchaseNote);

            // Only create inventory transactions if goods are actually received (Paid = received + paid)
            // Draft notes do NOT create stock yet
            if (initialStatus == PurchaseNoteStatus.Paid)
                await CreateInventoryTransactionsAsync(created);

            return MapToDto(created);
        }

        // ── Update ────────────────────────────────────────────────────────────────────

        public async Task<PurchaseNoteDto> UpdatePurchaseNoteAsync(Guid id, UpdatePurchaseNoteDto updateDto)
        {
            PurchaseNote? purchaseNote = await purchaseNoteRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Purchase note with ID {id} not found");

            if (purchaseNote.Status == PurchaseNoteStatus.Paid)
                throw new InvalidOperationException(
                    $"Purchase note {purchaseNote.NoteNumber} is Paid and cannot be edited.");

            if (purchaseNote.Status == PurchaseNoteStatus.Cancelled)
                throw new InvalidOperationException(
                    $"Purchase note {purchaseNote.NoteNumber} is Cancelled and cannot be edited.");

            if (!await individualRepository.ExistsAsync(updateDto.IndividualId))
                throw new KeyNotFoundException($"Individual with ID {updateDto.IndividualId} not found");

            if (!await warehouseRepository.ExistsAsync(updateDto.WarehouseId))
                throw new KeyNotFoundException($"Warehouse with ID {updateDto.WarehouseId} not found");

            purchaseNote.IndividualId = updateDto.IndividualId;
            purchaseNote.WarehouseId = updateDto.WarehouseId;
            purchaseNote.PurchaseDate = updateDto.PurchaseDate;
            purchaseNote.Notes = updateDto.Notes;

            // Line items only editable in Draft — Pending locks them since stock already moved
            if (purchaseNote.Status == PurchaseNoteStatus.Draft)
            {
                var productIds = updateDto.LineItems.Select(li => li.ProductId).ToList();
                if (!await productRepository.AllExistAsync(productIds))
                    throw new KeyNotFoundException("One or more products in the line items were not found");

                purchaseNote.LineItems.Clear();
                foreach (UpdatePurchaseNoteLineDto lineDto in updateDto.LineItems)
                {
                    purchaseNote.LineItems.Add(new PurchaseNoteLine
                    {
                        ProductId = lineDto.ProductId,
                        Description = lineDto.Description,
                        Quantity = lineDto.Quantity,
                        UnitPrice = lineDto.UnitPrice
                    });
                }

                purchaseNote.SubTotal = purchaseNote.LineItems.Sum(li => li.Amount);
                purchaseNote.TotalAmount = purchaseNote.SubTotal;
            }

            PurchaseNote updated = await purchaseNoteRepository.UpdateAsync(purchaseNote);
            return MapToDto(updated);
        }

        // ── Status transitions ────────────────────────────────────────────────────────

        /// <summary>Draft → Pending. Creates inventory transactions.</summary>
        public async Task<PurchaseNoteDto> ReceiveAsync(Guid id)
        {
            PurchaseNote? note = await purchaseNoteRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Purchase note with ID {id} not found");

            if (note.Status != PurchaseNoteStatus.Draft)
                throw new InvalidOperationException(
                    $"Purchase note {note.NoteNumber} must be in Draft to receive goods (current: {note.Status}).");

            note.Status = PurchaseNoteStatus.Pending;

            PurchaseNote updated = await purchaseNoteRepository.UpdateAsync(note);
            await CreateInventoryTransactionsAsync(updated);

            return MapToDto(updated);
        }

        /// <summary>Pending → Paid. Financial settlement only — no stock change.</summary>
        public async Task<PurchaseNoteDto> MarkAsPaidAsync(Guid id)
        {
            PurchaseNote? note = await purchaseNoteRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Purchase note with ID {id} not found");

            if (note.Status != PurchaseNoteStatus.Pending)
                throw new InvalidOperationException(
                    $"Purchase note {note.NoteNumber} must be Pending to mark as paid (current: {note.Status}).");

            note.Status = PurchaseNoteStatus.Paid;
            note.PaidDate = DateTime.UtcNow;

            PurchaseNote updated = await purchaseNoteRepository.UpdateAsync(note);
            return MapToDto(updated);
        }

        /// <summary>Pending → Draft. Reverses inventory transactions.</summary>
        public async Task<PurchaseNoteDto> RevertToDraftAsync(Guid id)
        {
            PurchaseNote? note = await purchaseNoteRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Purchase note with ID {id} not found");

            if (note.Status != PurchaseNoteStatus.Pending)
                throw new InvalidOperationException(
                    $"Purchase note {note.NoteNumber} must be Pending to revert to Draft (current: {note.Status}).");

            note.Status = PurchaseNoteStatus.Draft;

            PurchaseNote updated = await purchaseNoteRepository.UpdateAsync(note);

            await inventoryService.ReverseTransactionsForDocumentAsync(
                id,
                DocumentType,
                $"{localizationService.GetString("PurchaseNoteRevertedToDraft")}: {note.NoteNumber}");

            return MapToDto(updated);
        }

        /// <summary>Draft or Pending → Cancelled. Reverses stock if was Pending.</summary>
        public async Task<PurchaseNoteDto> CancelAsync(Guid id)
        {
            PurchaseNote? note = await purchaseNoteRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Purchase note with ID {id} not found");

            if (note.Status != PurchaseNoteStatus.Draft && note.Status != PurchaseNoteStatus.Pending)
                throw new InvalidOperationException(
                    $"Purchase note {note.NoteNumber} cannot be cancelled (current: {note.Status}).");

            bool wasP = note.Status == PurchaseNoteStatus.Pending;

            note.Status = PurchaseNoteStatus.Cancelled;

            PurchaseNote updated = await purchaseNoteRepository.UpdateAsync(note);

            if (wasP)
            {
                await inventoryService.ReverseTransactionsForDocumentAsync(
                    id,
                    DocumentType,
                    $"{localizationService.GetString("PurchaseNoteCancelled")}: {note.NoteNumber}");
            }

            return MapToDto(updated);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────────

        private async Task CreateInventoryTransactionsAsync(PurchaseNote purchaseNote)
        {
            foreach (PurchaseNoteLine line in purchaseNote.LineItems)
            {
                await inventoryService.CreateTransactionAsync(
                    new DTOs.InventoryTransaction.CreateInventoryTransactionDto
                    {
                        ProductId = line.ProductId,
                        WarehouseId = purchaseNote.WarehouseId,
                        Type = InventoryTransactionType.Inbound,
                        Quantity = line.Quantity,
                        SourceDocumentId = purchaseNote.Id,
                        SourceDocumentType = DocumentType,
                        Note = $"{localizationService.GetString("PurchaseFrom")} {purchaseNote.Individual.FullName} - {purchaseNote.NoteNumber}"
                    });
            }
        }

        private static PurchaseNoteDto MapToDto(PurchaseNote note) => new()
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
