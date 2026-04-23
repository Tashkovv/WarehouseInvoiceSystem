namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.DTOs.Dashboard;
    using WarehouseInvoiceSystem.Application.DTOs.InventoryTransaction;
    using WarehouseInvoiceSystem.Application.DTOs.PurchaseNote;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Domain.Queries.Results;

    public class PurchaseNoteService(
        IPurchaseNoteRepository purchaseNoteRepository,
        IIndividualRepository individualRepository,
        IWarehouseRepository warehouseRepository,
        IProductRepository productRepository,
        IInventoryService inventoryService,
        ILocalizationService localizationService,
        IInventoryTransactionRepository transactionRepository) : IPurchaseNoteService
    {
        private const string DocumentType = "PurchaseNote";

        // ── Queries ───────────────────────────────────────────────────────────────────

        public async Task<IEnumerable<PurchaseNoteDto>> GetAllPurchaseNotesAsync(CancellationToken ct = default)
        {
            IEnumerable<PurchaseNote> notes = await purchaseNoteRepository.GetAllAsync(ct);
            return notes.Select(MapToDto);
        }

        public async Task<PagedResult<PurchaseNoteDto>> GetPagedAsync(GetPurchaseNotesQuery query, CancellationToken ct = default)
        {
            PagedResult<PurchaseNote> result = await purchaseNoteRepository.GetPagedAsync(query, ct);
            return new PagedResult<PurchaseNoteDto>
            {
                Items = [.. result.Items.Select(MapToDto)],
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<IEnumerable<PurchaseNoteDto>> GetAllFilteredAsync(GetPurchaseNotesQuery query, CancellationToken ct = default)
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
            PagedResult<PurchaseNote> result = await purchaseNoteRepository.GetPagedAsync(exportQuery, ct);
            return result.Items.Select(MapToDto);
        }

        public async Task<PurchaseNoteDto?> GetPurchaseNoteByIdAsync(Guid id, CancellationToken ct = default)
        {
            PurchaseNote? note = await purchaseNoteRepository.GetByIdAsync(id, ct);
            return note == null ? null : MapToDto(note);
        }

        public async Task<PurchaseNoteDto?> GetPurchaseNoteByNumberAsync(string noteNumber, CancellationToken ct = default)
        {
            PurchaseNote? note = await purchaseNoteRepository.GetByNoteNumberAsync(noteNumber, ct);
            return note == null ? null : MapToDto(note);
        }

        public async Task<IEnumerable<PurchaseNoteDto>> GetPurchaseNotesByIndividualAsync(Guid individualId, CancellationToken ct = default)
        {
            IEnumerable<PurchaseNote> notes = await purchaseNoteRepository.GetByIndividualIdAsync(individualId, ct);
            return notes.Select(MapToDto);
        }

        public async Task<IEnumerable<PurchaseNoteDto>> GetPurchaseNotesByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
        {
            IEnumerable<PurchaseNote> notes = await purchaseNoteRepository.GetByDateRangeAsync(startDate, endDate, ct);
            return notes.Select(MapToDto);
        }

        public async Task<IEnumerable<PurchaseNoteDto>> GetPurchaseNotesByStatusAsync(PurchaseNoteStatus status, CancellationToken ct = default)
        {
            IEnumerable<PurchaseNote> notes = await purchaseNoteRepository.GetByStatusAsync(status, ct);
            return notes.Select(MapToDto);
        }

        // ── Create ────────────────────────────────────────────────────────────────────

        public async Task<Guid> CreatePurchaseNoteAsync(CreatePurchaseNoteDto createDto)
        {
            if (!await individualRepository.ExistsAsync(createDto.IndividualId))
                throw new KeyNotFoundException($"Individual with ID {createDto.IndividualId} not found");

            if (!await warehouseRepository.ExistsAsync(createDto.WarehouseId))
                throw new KeyNotFoundException($"Warehouse with ID {createDto.WarehouseId} not found");

            if (createDto.LineItems?.Count > 0)
            {
                var productIds = createDto.LineItems.Select(li => li.ProductId).ToList();
                if (!await productRepository.AllExistAsync(productIds))
                    throw new KeyNotFoundException("One or more products in the line items were not found");
            }

            string noteNumber = await purchaseNoteRepository.GenerateNoteNumberAsync();

            List<PurchaseNoteLine> lineItems = [.. (createDto.LineItems ?? []).Select(li => new PurchaseNoteLine
            {
                ProductId = li.ProductId,
                Description = li.Description,
                GrossQuantity = li.GrossQuantity,
                KaloPercentage = li.KaloPercentage,
                Quantity = CalculateNetQuantity(li.GrossQuantity, li.KaloPercentage),
                UnitPrice = li.UnitPrice,
                CreatedAt = DateTime.UtcNow,
            })];

            decimal subTotal = lineItems.Sum(li => li.Amount);

            PurchaseNote purchaseNote = new()
            {
                NoteNumber = noteNumber,
                IndividualId = createDto.IndividualId,
                WarehouseId = createDto.WarehouseId,
                PurchaseDate = createDto.PurchaseDate,
                SubTotal = subTotal,
                TotalAmount = subTotal,
                Status = PurchaseNoteStatus.Draft,
                Notes = createDto.Notes,
                LineItems = lineItems
            };

            await purchaseNoteRepository.CreateAsync(purchaseNote);
            return purchaseNote.Id;
        }

        public async Task<Guid> DuplicatePurchaseNoteAsync(Guid sourceId)
        {
            PurchaseNote? source = await purchaseNoteRepository.GetByIdAsync(sourceId)
                ?? throw new KeyNotFoundException($"Purchase note with ID {sourceId} not found");

            string noteNumber = await purchaseNoteRepository.GenerateNoteNumberAsync();

            List<PurchaseNoteLine> lineItems = [.. source.LineItems.Select(li => new PurchaseNoteLine
            {
                ProductId = li.ProductId,
                Description = li.Description,
                GrossQuantity = li.GrossQuantity,
                KaloPercentage = li.KaloPercentage,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                CreatedAt = DateTime.UtcNow,
            })];

            decimal subTotal = lineItems.Sum(li => li.Amount);

            PurchaseNote copy = new()
            {
                NoteNumber = noteNumber,
                IndividualId = source.IndividualId,
                WarehouseId = source.WarehouseId,
                PurchaseDate = DateTime.UtcNow.Date,
                SubTotal = subTotal,
                TotalAmount = subTotal,
                Status = PurchaseNoteStatus.Draft,
                Notes = source.Notes,
                LineItems = lineItems
            };

            await purchaseNoteRepository.CreateAsync(copy);
            return copy.Id;
        }

        // ── Update ────────────────────────────────────────────────────────────────────

        public async Task UpdatePurchaseNoteAsync(Guid id, UpdatePurchaseNoteDto updateDto)
        {
            if (updateDto.LineItems == null || updateDto.LineItems.Count == 0)
                throw new InvalidOperationException("A purchase note must have at least one line item.");

            PurchaseNote? purchaseNote = await purchaseNoteRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Purchase note with ID {id} not found");

            if (purchaseNote.Status != PurchaseNoteStatus.Draft)
                throw new InvalidOperationException(
                    $"Purchase note {purchaseNote.NoteNumber} cannot be fully edited (current: {purchaseNote.Status}). Only Draft notes can be edited.");

            if (!await individualRepository.ExistsAsync(updateDto.IndividualId))
                throw new KeyNotFoundException($"Individual with ID {updateDto.IndividualId} not found");

            if (!await warehouseRepository.ExistsAsync(updateDto.WarehouseId))
                throw new KeyNotFoundException($"Warehouse with ID {updateDto.WarehouseId} not found");

            var productIds = updateDto.LineItems.Select(li => li.ProductId).ToList();
            if (!await productRepository.AllExistAsync(productIds))
                throw new KeyNotFoundException("One or more products in the line items were not found");

            purchaseNote.IndividualId = updateDto.IndividualId;
            purchaseNote.WarehouseId = updateDto.WarehouseId;
            purchaseNote.PurchaseDate = updateDto.PurchaseDate;
            purchaseNote.Notes = updateDto.Notes;

            MergeLineItems(purchaseNote, updateDto.LineItems);

            await purchaseNoteRepository.UpdateAsync(purchaseNote);
        }

        // ── 3-way line item merge ─────────────────────────────────────────────────────
        // Update existing lines, remove deleted ones, insert new ones
        private static void MergeLineItems(PurchaseNote purchaseNote, IEnumerable<UpdatePurchaseNoteLineDto> incoming)
        {
            DateTime now = DateTime.UtcNow;
            HashSet<Guid> incomingIds = incoming
                .Where(li => li.Id != Guid.Empty)
                .Select(li => li.Id)
                .ToHashSet();

            // Lines absent from the incoming DTO were removed by the user — soft-delete them
            foreach (PurchaseNoteLine existing in purchaseNote.LineItems.Where(li => !incomingIds.Contains(li.Id)))
                existing.DeletedOn = now;

            // Update existing or insert new
            Dictionary<Guid, PurchaseNoteLine> existingById = purchaseNote.LineItems.ToDictionary(li => li.Id);
            List<PurchaseNoteLine> newLines = [];

            foreach (UpdatePurchaseNoteLineDto li in incoming)
            {
                if (li.Id != Guid.Empty && existingById.TryGetValue(li.Id, out PurchaseNoteLine? existing))
                {
                    existing.ProductId = li.ProductId;
                    existing.Description = li.Description;
                    existing.GrossQuantity = li.GrossQuantity;
                    existing.KaloPercentage = li.KaloPercentage;
                    existing.Quantity = CalculateNetQuantity(li.GrossQuantity, li.KaloPercentage);
                    existing.UnitPrice = li.UnitPrice;
                }
                else
                {
                    newLines.Add(new PurchaseNoteLine
                    {
                        ProductId = li.ProductId,
                        Description = li.Description,
                        GrossQuantity = li.GrossQuantity,
                        KaloPercentage = li.KaloPercentage,
                        Quantity = CalculateNetQuantity(li.GrossQuantity, li.KaloPercentage),
                        UnitPrice = li.UnitPrice
                    });
                }
            }

            foreach (PurchaseNoteLine line in newLines)
                purchaseNote.LineItems.Add(line);

            // Recalculate totals from active lines only
            decimal subTotal = purchaseNote.LineItems.Where(li => li.DeletedOn == null).Sum(li => li.Amount);
            purchaseNote.SubTotal = subTotal;
            purchaseNote.TotalAmount = subTotal;
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

            if (note.LineItems.Count == 0)
                throw new InvalidOperationException("CannotReceiveEmptyNote");

            PurchaseNoteStatus previousStatus = note.Status;
            note.Status = PurchaseNoteStatus.Pending;

            await purchaseNoteRepository.UpdateAsync(note);

            try
            {
                await inventoryService.SoftDeleteTransactionsForDocumentAsync(note.Id, DocumentType);
                await inventoryService.SoftDeleteReversalForDocumentAsync(note.Id, DocumentType);
                await CreateInventoryTransactionsAsync(note, note.Individual.FullName);
            }
            catch (Exception)
            {
                note.Status = previousStatus;
                await purchaseNoteRepository.UpdateAsync(note);
                throw;
            }

            return MapToDto(note);
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

            await purchaseNoteRepository.UpdateAsync(note);
            return MapToDto(note);
        }

        /// <summary>Pending → Draft. Soft-deletes inventory transactions (undoes stock, no reversal trail).</summary>
        public async Task<PurchaseNoteDto> RevertToDraftAsync(Guid id)
        {
            PurchaseNote? note = await purchaseNoteRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Purchase note with ID {id} not found");

            if (note.Status != PurchaseNoteStatus.Pending)
                throw new InvalidOperationException(
                    $"Purchase note {note.NoteNumber} must be Pending to revert to Draft (current: {note.Status}).");

            PurchaseNoteStatus previousStatus = note.Status;
            note.Status = PurchaseNoteStatus.Draft;

            await purchaseNoteRepository.UpdateAsync(note);

            try
            {
                await inventoryService.SoftDeleteTransactionsForDocumentAsync(note.Id, DocumentType);
            }
            catch (Exception)
            {
                note.Status = previousStatus;
                await purchaseNoteRepository.UpdateAsync(note);
                throw;
            }

            return MapToDto(note);
        }

        /// <summary>Draft or Pending → Cancelled. Reverses stock if was Pending.</summary>
        public async Task<PurchaseNoteDto> CancelAsync(Guid id)
        {
            PurchaseNote? note = await purchaseNoteRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Purchase note with ID {id} not found");

            if (note.Status != PurchaseNoteStatus.Draft && note.Status != PurchaseNoteStatus.Pending)
                throw new InvalidOperationException(
                    $"Purchase note {note.NoteNumber} cannot be cancelled (current: {note.Status}).");

            bool wasPending = note.Status == PurchaseNoteStatus.Pending;
            PurchaseNoteStatus previousStatus = note.Status;

            note.Status = PurchaseNoteStatus.Cancelled;
            await purchaseNoteRepository.UpdateAsync(note);

            if (wasPending)
            {
                try
                {
                    await inventoryService.ReverseTransactionsForDocumentAsync(
                        id,
                        DocumentType,
                        $"{localizationService.GetString("PurchaseNoteCancelled")}: {note.NoteNumber}");
                }
                catch (Exception)
                {
                    note.Status = previousStatus;
                    await purchaseNoteRepository.UpdateAsync(note);
                    throw;
                }
            }

            return MapToDto(note);
        }

        public async Task UpdateNotesAsync(Guid id, string? notes, CancellationToken ct = default)
        {
            PurchaseNote? purchaseNote = await purchaseNoteRepository.GetByIdAsync(id, ct)
                ?? throw new KeyNotFoundException($"Purchase note with ID {id} not found");

            purchaseNote.Notes = notes;
            await purchaseNoteRepository.UpdateAsync(purchaseNote);
        }

        public async Task UpdatePurchaseDateAsync(Guid id, DateTime purchaseDate)
        {
            PurchaseNote? note = await purchaseNoteRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Purchase note with ID {id} not found");

            if (note.Status != PurchaseNoteStatus.Draft)
                throw new InvalidOperationException($"Purchase note {note.NoteNumber} must be Draft to edit.");

            note.PurchaseDate = purchaseDate;
            await purchaseNoteRepository.UpdateAsync(note);
        }

        public async Task UpdateWarehouseAsync(Guid id, Guid warehouseId)
        {
            PurchaseNote? note = await purchaseNoteRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Purchase note with ID {id} not found");

            if (note.Status != PurchaseNoteStatus.Draft)
                throw new InvalidOperationException($"Purchase note {note.NoteNumber} must be Draft to edit.");

            if (!await warehouseRepository.ExistsAsync(warehouseId))
                throw new KeyNotFoundException($"Warehouse with ID {warehouseId} not found");

            note.WarehouseId = warehouseId;
            await purchaseNoteRepository.UpdateAsync(note);
        }

        public async Task UpdateIndividualAsync(Guid id, Guid individualId)
        {
            PurchaseNote? note = await purchaseNoteRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Purchase note with ID {id} not found");

            if (note.Status != PurchaseNoteStatus.Draft)
                throw new InvalidOperationException($"Purchase note {note.NoteNumber} must be Draft to edit.");

            if (!await individualRepository.ExistsAsync(individualId))
                throw new KeyNotFoundException($"Individual with ID {individualId} not found");

            note.IndividualId = individualId;
            await purchaseNoteRepository.UpdateAsync(note);
        }

        // ── Delete ────────────────────────────────────────────────────────────────────

        /// <summary>Soft-deletes a Cancelled purchase note.</summary>
        public async Task<bool> DeletePurchaseNoteAsync(Guid id)
        {
            PurchaseNote? note = await purchaseNoteRepository.GetByIdAsync(id);
            if (note == null) return false;

            if (note.Status != PurchaseNoteStatus.Cancelled)
                throw new InvalidOperationException(
                    $"Purchase note {note.NoteNumber} can only be deleted when Cancelled (current: {note.Status}).");

            // Clean up inventory transactions created during receive and reversal during cancel.
            // Stock is already correct after cancellation, so no stock adjustment — just remove the records.
            await transactionRepository.SoftDeleteByDocumentAsync(id, DocumentType);
            await transactionRepository.SoftDeleteByDocumentAsync(id, $"{DocumentType}_Reversal");

            return await purchaseNoteRepository.DeleteAsync(id);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────────

        private static decimal CalculateNetQuantity(decimal grossQuantity, decimal kaloPercentage)
            => Math.Round(grossQuantity * (1 - kaloPercentage / 100m), MidpointRounding.AwayFromZero);

        private async Task CreateInventoryTransactionsAsync(PurchaseNote purchaseNote, string individualName)
        {
            string note = $"{localizationService.GetString("PurchaseFrom")} {individualName} - {purchaseNote.NoteNumber}";

            IEnumerable<CreateInventoryTransactionDto> items = purchaseNote.LineItems.Select(line =>
                new CreateInventoryTransactionDto
                {
                    ProductId = line.ProductId,
                    WarehouseId = purchaseNote.WarehouseId,
                    Type = InventoryTransactionType.Inbound,
                    Quantity = line.Quantity,
                    SourceDocumentId = purchaseNote.Id,
                    SourceDocumentType = DocumentType,
                    Note = note
                });

            await inventoryService.CreateBatchAsync(purchaseNote.WarehouseId, items);
        }

        // ── Dashboard targeted queries ────────────────────────────────────────────────

        public async Task<IEnumerable<PurchaseNoteDto>> GetRecentAsync(int count, CancellationToken ct = default)
        {
            IEnumerable<PurchaseNote> notes = await purchaseNoteRepository.GetRecentAsync(count, ct);
            return notes.Select(MapToDto);
        }

        public async Task<IEnumerable<PurchaseNoteDto>> GetByPurchaseDateAsync(DateTime date, CancellationToken ct = default)
        {
            IEnumerable<PurchaseNote> notes = await purchaseNoteRepository.GetByPurchaseDateAsync(date, ct);
            return notes.Select(MapToDto);
        }

        public async Task<IEnumerable<PurchaseNoteDto>> GetByPurchaseDateMonthAsync(int year, int month, CancellationToken ct = default)
        {
            IEnumerable<PurchaseNote> notes = await purchaseNoteRepository.GetByPurchaseDateMonthAsync(year, month, ct);
            return notes.Select(MapToDto);
        }

        public Task<(int unpaidCount, decimal unpaidAmount)> GetOutstandingPositionAsync(CancellationToken ct = default)
            => purchaseNoteRepository.GetOutstandingPositionAsync(ct);

        public async Task<IEnumerable<PartnerSummaryDto>> GetTopVendorsBySpendAsync(DateTime from, DateTime to, int topCount, CancellationToken ct = default)
        {
            IEnumerable<PartnerSummaryResult> results =
                await purchaseNoteRepository.GetTopVendorsBySpendAsync(from, to, topCount, ct);
            return results.Select(r => new PartnerSummaryDto
            {
                PartnerId = r.PartnerId,
                PartnerName = r.PartnerName,
                DocumentCount = r.Count,
                TotalAmount = r.Amount
            });
        }

        public async Task<IEnumerable<PartnerAttentionDto>> GetUnpaidVendorSummariesAsync(CancellationToken ct = default)
        {
            IEnumerable<PartnerSummaryResult> results =
                await purchaseNoteRepository.GetUnpaidVendorSummariesAsync(ct);
            return results.Select(r => new PartnerAttentionDto
            {
                PartnerId = r.PartnerId,
                PartnerName = r.PartnerName,
                Count = r.Count,
                Amount = r.Amount
            });
        }

        public async Task<IEnumerable<ProductMovementDto>> GetProductPurchasesByWarehouseAsync(
            Guid warehouseId, DateTime from, DateTime to, CancellationToken ct = default)
        {
            IEnumerable<ProductMovementResult> results =
                await purchaseNoteRepository.GetProductPurchasesByWarehouseAsync(warehouseId, from, to, ct);
            return results.Select(r => new ProductMovementDto
            {
                ProductId = r.ProductId,
                Quantity = r.Quantity,
                TotalAmount = r.TotalAmount
            });
        }

        public async Task<IEnumerable<PurchaseNoteDto>> GetTopUnpaidAsync(
            Guid? warehouseId, int top, CancellationToken ct = default)
        {
            IEnumerable<PurchaseNote> notes = await purchaseNoteRepository.GetTopUnpaidAsync(warehouseId, top, ct);
            return notes.Select(MapToDto);
        }

        public async Task<IEnumerable<ProductMovementWithNameDto>> GetTopProductPurchasesAsync(
            Guid warehouseId, DateTime from, DateTime to, int top, CancellationToken ct = default)
        {
            IEnumerable<ProductMovementWithNameResult> results =
                await purchaseNoteRepository.GetTopProductPurchasesByWarehouseAsync(warehouseId, from, to, top, ct);
            return results.Select(r => new ProductMovementWithNameDto
            {
                ProductId = r.ProductId,
                ProductCode = r.ProductCode,
                ProductName = r.ProductName,
                Unit = r.Unit,
                Quantity = r.Quantity,
                TotalValue = r.TotalAmount
            });
        }

        public Task<DayPurchaseNoteSummaryResult> GetDayIssuedSummaryAsync(DateTime date, CancellationToken ct = default)
            => purchaseNoteRepository.GetDayIssuedSummaryAsync(date, ct);

        public Task<DayPurchaseNoteSummaryResult> GetMonthIssuedSummaryAsync(int year, int month, CancellationToken ct = default)
            => purchaseNoteRepository.GetMonthIssuedSummaryAsync(year, month, ct);

        public Task<DayPurchaseNoteSummaryResult> GetYearIssuedSummaryAsync(int year, CancellationToken ct = default)
            => purchaseNoteRepository.GetYearIssuedSummaryAsync(year, ct);

        public Task<DayPurchaseNoteSummaryResult> GetRangeIssuedSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default)
            => purchaseNoteRepository.GetRangeIssuedSummaryAsync(from, to, ct);

        private static PurchaseNoteDto MapToDto(PurchaseNote note) => new()
        {
            Id = note.Id,
            NoteNumber = note.NoteNumber,
            IndividualId = note.IndividualId,
            IndividualFullName = note.Individual?.FullName ?? string.Empty,
            IndividualIdentificationNumber = note.Individual?.IdentificationNumber ?? string.Empty,
            IndividualAddress = note.Individual?.Address,
            IndividualBankAccount = note.Individual?.BankAccount,
            WarehouseId = note.WarehouseId,
            WarehouseName = note.Warehouse.Name,
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
                GrossQuantity = li.GrossQuantity,
                KaloPercentage = li.KaloPercentage,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                Amount = li.Amount
            })]
        };
    }
}