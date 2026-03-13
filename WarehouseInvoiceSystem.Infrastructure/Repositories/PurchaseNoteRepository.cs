namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    public class PurchaseNoteRepository(IDbContextFactory<ApplicationDbContext> factory)
        : BaseRepository(factory), IPurchaseNoteRepository
    {
        public Task<IEnumerable<PurchaseNote>> GetAllAsync() =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<PurchaseNote>)await All<PurchaseNote>(context)
                    .Include(pn => pn.Individual)
                    .Include(pn => pn.Warehouse)
                    .Include(pn => pn.LineItems)
                        .ThenInclude(li => li.Product)
                    .OrderByDescending(pn => pn.PurchaseDate)
                    .ToListAsync();
            });

        public Task<PagedResult<PurchaseNote>> GetPagedAsync(GetPurchaseNotesQuery query) =>
            WithContextAsync(async context =>
            {
                IQueryable<PurchaseNote> q = ApplyFilters(
                    All<PurchaseNote>(context)
                        .Include(p => p.Individual)
                        .Include(p => p.Warehouse)
                        .Include(p => p.LineItems)
                            .ThenInclude(li => li.Product),
                    query);

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
            });

        public Task<PurchaseNote?> GetByIdAsync(Guid id) =>
            WithContextAsync(context =>
                All<PurchaseNote>(context)
                    .Include(pn => pn.Individual)
                    .Include(pn => pn.Warehouse)
                    .Include(pn => pn.LineItems)
                        .ThenInclude(li => li.Product)
                    .FirstOrDefaultAsync(pn => pn.Id == id));

        public Task<PurchaseNote?> GetByNoteNumberAsync(string noteNumber) =>
            WithContextAsync(context =>
                All<PurchaseNote>(context)
                    .Include(pn => pn.Individual)
                    .Include(pn => pn.Warehouse)
                    .Include(pn => pn.LineItems)
                        .ThenInclude(li => li.Product)
                    .FirstOrDefaultAsync(pn => pn.NoteNumber == noteNumber));

        public Task<IEnumerable<PurchaseNote>> GetByIndividualIdAsync(Guid individualId) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<PurchaseNote>)await All<PurchaseNote>(context)
                    .Where(pn => pn.IndividualId == individualId)
                    .Include(pn => pn.Individual)
                    .Include(pn => pn.LineItems)
                        .ThenInclude(li => li.Product)
                    .OrderByDescending(pn => pn.PurchaseDate)
                    .ToListAsync();
            });

        public Task<IEnumerable<PurchaseNote>> GetByDateRangeAsync(DateTime startDate, DateTime endDate) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<PurchaseNote>)await All<PurchaseNote>(context)
                    .Where(pn => pn.PurchaseDate.Date >= startDate.Date &&
                                 pn.PurchaseDate.Date <= endDate.Date)
                    .Include(pn => pn.Individual)
                    .Include(pn => pn.Warehouse)
                    .OrderByDescending(pn => pn.PurchaseDate)
                    .ToListAsync();
            });

        public Task<IEnumerable<PurchaseNote>> GetByStatusAsync(PurchaseNoteStatus status) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<PurchaseNote>)await All<PurchaseNote>(context)
                    .Where(pn => pn.Status == status)
                    .Include(pn => pn.Individual)
                    .Include(pn => pn.LineItems)
                        .ThenInclude(li => li.Product)
                    .OrderByDescending(pn => pn.PurchaseDate)
                    .ToListAsync();
            });

        public Task<IEnumerable<PurchaseNoteLine>> GetLineItemsByProductIdAsync(Guid productId) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<PurchaseNoteLine>)await All<PurchaseNoteLine>(context)
                    .Where(li => li.ProductId == productId &&
                                 li.PurchaseNote.DeletedOn == null &&
                                 li.PurchaseNote.Status != PurchaseNoteStatus.Cancelled)
                    .Include(li => li.PurchaseNote)
                        .ThenInclude(pn => pn.Individual)
                    .Include(li => li.PurchaseNote)
                        .ThenInclude(pn => pn.Warehouse)
                    .OrderByDescending(li => li.PurchaseNote.PurchaseDate)
                    .ToListAsync();
            });

        public Task<PagedResult<PurchaseNoteLine>> GetPagedLineItemsByProductIdAsync(GetProductHistoryQuery query) =>
            WithContextAsync(async context =>
            {
                IQueryable<PurchaseNoteLine> q = ApplyLineItemFilters(
                    All<PurchaseNoteLine>(context)
                        .Include(li => li.PurchaseNote)
                            .ThenInclude(pn => pn.Individual)
                        .Include(li => li.PurchaseNote)
                            .ThenInclude(pn => pn.Warehouse),
                    query);

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
            });

        public Task<string> GenerateNoteNumberAsync() =>
            WithContextAsync(async context =>
            {
                string? lastNoteNumber = await context.PurchaseNotes
                    .Where(pn => pn.NoteNumber.StartsWith("OB-"))
                    .OrderByDescending(pn => pn.NoteNumber)
                    .Select(pn => pn.NoteNumber)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(lastNoteNumber))
                    return "OB-000001";

                string numberPart = lastNoteNumber.Replace("OB-", "");
                if (int.TryParse(numberPart, out int lastNumber))
                    return $"OB-{(lastNumber + 1):D6}";

                return "OB-000001";
            });

        public Task<bool> ExistsAsync(Guid id) =>
            WithContextAsync(context =>
                All<PurchaseNote>(context).AnyAsync(pn => pn.Id == id));

        public Task CreateAsync(PurchaseNote purchaseNote) =>
            WithContextAsync(async context =>
            {
                purchaseNote.CreatedAt = DateTime.UtcNow;
                context.PurchaseNotes.Add(purchaseNote);
                await SaveAsync(context);
            });

        public Task UpdateAsync(PurchaseNote purchaseNote) =>
            WithContextAsync(async context =>
            {
                context.PurchaseNotes.Update(purchaseNote);
                await SaveAsync(context);
            });

        public Task<bool> DeleteAsync(Guid id) =>
            WithContextAsync(async context =>
            {
                PurchaseNote? purchaseNote = await context.PurchaseNotes.FindAsync(id);
                if (purchaseNote == null)
                    return false;

                purchaseNote.DeletedOn = DateTime.UtcNow;
                await SaveAsync(context);
                return true;
            });

        private static IQueryable<PurchaseNote> ApplyFilters(IQueryable<PurchaseNote> q, GetPurchaseNotesQuery query)
        {
            if (query.Status.HasValue)
                q = q.Where(p => p.Status == query.Status.Value);

            if (query.IndividualId.HasValue)
                q = q.Where(p => p.IndividualId == query.IndividualId.Value);

            else if (!string.IsNullOrWhiteSpace(query.IndividualName))
                q = q.Where(p =>
                    EF.Functions.ILike(p.Individual.FirstName, $"%{query.IndividualName}%") ||
                    EF.Functions.ILike(p.Individual.LastName, $"%{query.IndividualName}%"));

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
                string search = $"%{query.Search}%";
                q = q.Where(p =>
                    EF.Functions.ILike(p.NoteNumber, search) ||
                    EF.Functions.ILike(p.Individual.FirstName, search) ||
                    EF.Functions.ILike(p.Individual.LastName, search));
            }

            return q;
        }

        private static IQueryable<PurchaseNoteLine> ApplyLineItemFilters(IQueryable<PurchaseNoteLine> q, GetProductHistoryQuery query)
        {
            q = q.Where(li => li.ProductId == query.ProductId &&
                               li.PurchaseNote.DeletedOn == null &&
                               li.PurchaseNote.Status != PurchaseNoteStatus.Cancelled);

            if (query.WarehouseId.HasValue)
                q = q.Where(li => li.PurchaseNote.WarehouseId == query.WarehouseId.Value);

            if (!string.IsNullOrWhiteSpace(query.PartyName))
                q = q.Where(li => (li.PurchaseNote.Individual.FirstName + " " + li.PurchaseNote.Individual.LastName) == query.PartyName);

            if (query.DateFrom.HasValue)
                q = q.Where(li => li.PurchaseNote.PurchaseDate >= query.DateFrom.Value.Date);

            if (query.DateTo.HasValue)
                q = q.Where(li => li.PurchaseNote.PurchaseDate < query.DateTo.Value.Date.AddDays(1));

            return q;
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