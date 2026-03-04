namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.DTOs.PurchaseNote;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Interfaces;

    public class PurchaseNoteService(
        IPurchaseNoteRepository purchaseNoteRepository,
        IIndividualRepository individualRepository,
        IWarehouseRepository warehouseRepository,
        IProductRepository productRepository,
        IInventoryService inventoryService,
        ILocalizationService localizationService) : IPurchaseNoteService
    {
        public async Task<IEnumerable<PurchaseNoteDto>> GetAllPurchaseNotesAsync()
        {
            IEnumerable<PurchaseNote> notes = await purchaseNoteRepository.GetAllAsync();
            return notes.Select(MapToDto);
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

            // Validate warehouse if provided
            if (createDto.WarehouseId.HasValue && !await warehouseRepository.ExistsAsync(createDto.WarehouseId.Value))
                throw new KeyNotFoundException($"Warehouse with ID {createDto.WarehouseId} not found");

            // Validate products
            foreach (Guid productId in createDto.LineItems.Select(line => line.ProductId))
            {
                if (!await productRepository.ExistsAsync(productId))
                    throw new KeyNotFoundException($"Product with ID {productId} not found");
            }

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
                WarehouseId = createDto.WarehouseId,
                PurchaseDate = createDto.PurchaseDate,
                SubTotal = subTotal,
                TotalAmount = subTotal,
                Status = createDto.MarkAsPaid ? PurchaseNoteStatus.Paid : PurchaseNoteStatus.Completed,
                PaidDate = itemPaidDate,
                Notes = createDto.Notes,
                LineItems = lineItems
            };

            PurchaseNote created = await purchaseNoteRepository.CreateAsync(purchaseNote);

            // Create inventory transactions if completed/paid
            if (created.Status == PurchaseNoteStatus.Completed || created.Status == PurchaseNoteStatus.Paid)
            {
                await CreateInventoryTransactionsAsync(created);
            }

            return MapToDto(created);
        }

        public async Task<PurchaseNoteDto> UpdatePurchaseNoteAsync(Guid id, UpdatePurchaseNoteDto updateDto)
        {
            PurchaseNote? purchaseNote = await purchaseNoteRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Purchase note with ID {id} not found");

            // Validate individual exists
            if (!await individualRepository.ExistsAsync(updateDto.IndividualId))
                throw new KeyNotFoundException($"Individual with ID {updateDto.IndividualId} not found");

            // Validate warehouse if provided
            if (updateDto.WarehouseId.HasValue && !await warehouseRepository.ExistsAsync(updateDto.WarehouseId.Value))
                throw new KeyNotFoundException($"Warehouse with ID {updateDto.WarehouseId} not found");

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
            return MapToDto(updated);
        }

        public async Task<bool> DeletePurchaseNoteAsync(Guid id)
        {
            return await purchaseNoteRepository.DeleteAsync(id);
        }

        public async Task<PurchaseNoteDto> MarkAsPaidAsync(Guid id)
        {
            PurchaseNote? purchaseNote = await purchaseNoteRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Purchase note with ID {id} not found");

            purchaseNote.Status = PurchaseNoteStatus.Paid;
            purchaseNote.PaidDate = DateTime.UtcNow;

            PurchaseNote updated = await purchaseNoteRepository.UpdateAsync(purchaseNote);
            return MapToDto(updated);
        }

        private async Task CreateInventoryTransactionsAsync(PurchaseNote purchaseNote)
        {
            // Get default warehouse if not specified
            Guid warehouseId = purchaseNote.WarehouseId ?? (await warehouseRepository.GetDefaultWarehouseAsync())?.Id
                ?? throw new InvalidOperationException("No warehouse specified and no default warehouse found");

            // Create inbound transaction for each line item
            foreach (PurchaseNoteLine line in purchaseNote.LineItems)
            {
                await inventoryService.CreateTransactionAsync(new DTOs.InventoryTransaction.CreateInventoryTransactionDto
                {
                    ProductId = line.ProductId,
                    WarehouseId = warehouseId,
                    Type = InventoryTransactionType.Inbound,
                    Quantity = line.Quantity,
                    SourceDocumentId = purchaseNote.Id,
                    SourceDocumentType = "PurchaseNote",
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
                    ProductCode = li.Product?.Code ?? string.Empty,
                    ProductName = li.Product?.Name ?? string.Empty,
                    Description = li.Description,
                    Quantity = li.Quantity,
                    UnitPrice = li.UnitPrice,
                    Amount = li.Amount
                })]
            };
        }
    }
}