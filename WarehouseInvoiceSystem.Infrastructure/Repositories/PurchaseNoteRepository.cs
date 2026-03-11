namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
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

        public async Task<PagedResult<PurchaseNote>> GetPagedAsync(GetPurchaseNotesQuery query)
        {
            IQueryable<PurchaseNote> q = context.PurchaseNotes
                .Where(p => p.DeletedOn == null)
                .Include(p => p.Individual)
                .Include(p => p.Warehouse)
                .Include(p => p.LineItems)
                    .ThenInclude(li => li.Product);

            if (query.Status.HasValue)
                q = q.Where(p => p.Status == query.Status.Value);

            if (!string.IsNullOrWhiteSpace(query.IndividualName))
                q = q.Where(p => query.IndividualName.Contains(p.Individual.FirstName) ||
                                 query.IndividualName.Contains(p.Individual.LastName));

            if (query.AmountMin.HasValue)
                q = q.Where(p => p.TotalAmount >= query.AmountMin.Value);

            if (query.AmountMax.HasValue)
                q = q.Where(p => p.TotalAmount <= query.AmountMax.Value);

            if (query.DateFrom.HasValue)
                q = q.Where(p => p.PurchaseDate >= query.DateFrom.Value);

            if (query.DateTo.HasValue)
                q = q.Where(p => p.PurchaseDate <= query.DateTo.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                q = q.Where(p => p.NoteNumber.Contains(query.Search) ||
                                 p.Individual.FirstName.Contains(query.Search) ||
                                 p.Individual.LastName.Contains(query.Search) ||
                                 (query.Search.Contains(p.Individual.FirstName) ||
                                 query.Search.Contains(p.Individual.LastName)));
            }

            q = ApplySort(q, query.SortBy, query.SortAscending);

            int totalCount = await q.CountAsync();

            List<PurchaseNote> items = await q
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<PurchaseNote>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
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

        public async Task<PagedResult<PurchaseNoteLine>> GetPagedLineItemsByProductIdAsync(GetProductHistoryQuery query)
        {
            IQueryable<PurchaseNoteLine> q = context.PurchaseNoteLines
                .Where(li => li.ProductId == query.ProductId &&
                             li.DeletedOn == null &&
                             li.PurchaseNote.DeletedOn == null)
                .Include(li => li.PurchaseNote)
                    .ThenInclude(pn => pn.Individual)
                .Include(li => li.PurchaseNote)
                    .ThenInclude(pn => pn.Warehouse);

            if (query.WarehouseId.HasValue)
                q = q.Where(li => li.PurchaseNote.WarehouseId == query.WarehouseId.Value);

            // PartyName for purchase notes is Individual.FullName (FirstName + " " + LastName)
            if (!string.IsNullOrWhiteSpace(query.PartyName))
                q = q.Where(li => (li.PurchaseNote.Individual.FirstName + " " + li.PurchaseNote.Individual.LastName) == query.PartyName);

            if (query.DateFrom.HasValue)
                q = q.Where(li => li.PurchaseNote.PurchaseDate >= query.DateFrom.Value.Date);

            if (query.DateTo.HasValue)
                q = q.Where(li => li.PurchaseNote.PurchaseDate < query.DateTo.Value.Date.AddDays(1));

            q = q.OrderByDescending(li => li.PurchaseNote.PurchaseDate);

            int totalCount = await q.CountAsync();

            List<PurchaseNoteLine> items = await q
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<PurchaseNoteLine>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
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

        private static IQueryable<PurchaseNote> ApplySort(IQueryable<PurchaseNote> q, string? sortBy, bool ascending)
            => sortBy switch
            {
                "NoteNumber" => ascending ? q.OrderBy(p => p.NoteNumber) : q.OrderByDescending(p => p.NoteNumber),
                "IndividualLastName" => ascending ? q.OrderBy(p => p.Individual.LastName) : q.OrderByDescending(p => p.Individual.LastName),
                "PurchaseDate" => ascending ? q.OrderBy(p => p.PurchaseDate) : q.OrderByDescending(p => p.PurchaseDate),
                "TotalAmount" => ascending ? q.OrderBy(p => p.TotalAmount) : q.OrderByDescending(p => p.TotalAmount),
                _ => q.OrderByDescending(p => p.CreatedAt)
            };
    }
}