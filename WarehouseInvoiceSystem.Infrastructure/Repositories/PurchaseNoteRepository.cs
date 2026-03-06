namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    public class PurchaseNoteRepository(ApplicationDbContext context) : IPurchaseNoteRepository
    {
        public async Task<IEnumerable<PurchaseNote>> GetAllAsync()
        {
            return await context.PurchaseNotes
                .Where(pn => pn.DeletedOn == null)
                .Include(pn => pn.Individual)
                .Include(pn => pn.Warehouse)
                .Include(pn => pn.LineItems)
                    .ThenInclude(li => li.Product)
                .OrderByDescending(pn => pn.PurchaseDate)
                .ToListAsync();
        }

        public async Task<PurchaseNote?> GetByIdAsync(Guid id)
        {
            return await context.PurchaseNotes
                .Where(pn => pn.DeletedOn == null)
                .Include(pn => pn.Individual)
                .Include(pn => pn.Warehouse)
                .Include(pn => pn.LineItems)
                    .ThenInclude(li => li.Product)
                .FirstOrDefaultAsync(pn => pn.Id == id);
        }

        public async Task<PurchaseNote?> GetByNoteNumberAsync(string noteNumber)
        {
            return await context.PurchaseNotes
                .Where(pn => pn.DeletedOn == null)
                .Include(pn => pn.Individual)
                .Include(pn => pn.Warehouse)
                .Include(pn => pn.LineItems)
                    .ThenInclude(li => li.Product)
                .FirstOrDefaultAsync(pn => pn.NoteNumber == noteNumber);
        }

        public async Task<IEnumerable<PurchaseNote>> GetByIndividualIdAsync(Guid individualId)
        {
            return await context.PurchaseNotes
                .Where(pn => pn.DeletedOn == null && pn.IndividualId == individualId)
                .Include(pn => pn.Individual)
                .Include(pn => pn.LineItems)
                    .ThenInclude(li => li.Product)
                .OrderByDescending(pn => pn.PurchaseDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<PurchaseNote>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await context.PurchaseNotes
                .Where(pn => pn.DeletedOn == null &&
                            pn.PurchaseDate.Date >= startDate.Date &&
                            pn.PurchaseDate.Date <= endDate.Date)
                .Include(pn => pn.Individual)
                .Include(pn => pn.Warehouse)
                .OrderByDescending(pn => pn.PurchaseDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<PurchaseNote>> GetByStatusAsync(PurchaseNoteStatus status)
        {
            return await context.PurchaseNotes
                .Where(pn => pn.DeletedOn == null && pn.Status == status)
                .Include(pn => pn.Individual)
                .Include(pn => pn.LineItems)
                    .ThenInclude(li => li.Product)
                .OrderByDescending(pn => pn.PurchaseDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<PurchaseNoteLine>> GetLineItemsByProductIdAsync(Guid productId)
        {
            return await context.PurchaseNoteLines
                .Where(li => li.ProductId == productId &&
                             li.DeletedOn == null &&
                             li.PurchaseNote.DeletedOn == null)
                .Include(li => li.PurchaseNote)
                    .ThenInclude(pn => pn.Individual)
                .Include(li => li.PurchaseNote)
                    .ThenInclude(pn => pn.Warehouse)
                .OrderByDescending(li => li.PurchaseNote.PurchaseDate)
                .ToListAsync();
        }

        public async Task<string> GenerateNoteNumberAsync()
        {
            string? lastNoteNumber = await context.PurchaseNotes
                .Where(pn => pn.NoteNumber.StartsWith("OB-"))
                .OrderByDescending(pn => pn.NoteNumber)
                .Select(pn => pn.NoteNumber)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(lastNoteNumber))
            {
                return "OB-000001";
            }

            string numberPart = lastNoteNumber.Replace("OB-", "");
            if (int.TryParse(numberPart, out int lastNumber))
            {
                return $"OB-{(lastNumber + 1):D6}";
            }

            return "OB-000001";
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await context.PurchaseNotes
                .AnyAsync(pn => pn.Id == id && pn.DeletedOn == null);
        }

        public async Task<PurchaseNote> CreateAsync(PurchaseNote purchaseNote)
        {
            purchaseNote.CreatedAt = DateTime.UtcNow;

            context.PurchaseNotes.Add(purchaseNote);
            await context.SaveChangesAsync();

            return purchaseNote;
        }

        public async Task<PurchaseNote> UpdateAsync(PurchaseNote purchaseNote)
        {
            context.PurchaseNotes.Update(purchaseNote);
            await context.SaveChangesAsync();

            return purchaseNote;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            PurchaseNote? purchaseNote = await context.PurchaseNotes.FindAsync(id);
            if (purchaseNote == null)
                return false;

            purchaseNote.DeletedOn = DateTime.UtcNow;
            await context.SaveChangesAsync();

            return true;
        }
    }
}