namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Domain.Queries.Results;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    public class PurchaseNoteRepository(IDbContextFactory<ApplicationDbContext> factory)
        : BaseRepository(factory), IPurchaseNoteRepository
    {
        public Task<IEnumerable<PurchaseNote>> GetAllAsync(CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<PurchaseNote>)await All<PurchaseNote>(context)
                    .Include(pn => pn.Individual)
                    .Include(pn => pn.Warehouse)
                    .Include(pn => pn.LineItems)
                        .ThenInclude(li => li.Product)
                    .OrderByDescending(pn => pn.PurchaseDate)
                    .ToListAsync(ct);
            });

        public Task<PagedResult<PurchaseNote>> GetPagedAsync(GetPurchaseNotesQuery query, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                IQueryable<PurchaseNote> q = ApplyFilters(
                    All<PurchaseNote>(context)
                        .Include(p => p.Individual)
                        .Include(p => p.Warehouse),
                    query);

                q = ApplySort(q, query.SortBy, query.SortAscending);

                int totalCount = await q.CountAsync(ct);

                List<PurchaseNote> items = await q
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync(ct);

                return new PagedResult<PurchaseNote>
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = query.Page,
                    PageSize = query.PageSize
                };
            });

        public Task<PurchaseNote?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            WithContextAsync(context =>
                All<PurchaseNote>(context)
                    .Include(pn => pn.Individual)
                    .Include(pn => pn.Warehouse)
                    .Include(pn => pn.LineItems)
                        .ThenInclude(li => li.Product)
                    .FirstOrDefaultAsync(pn => pn.Id == id, ct));

        public Task<PurchaseNote?> GetByNoteNumberAsync(string noteNumber, CancellationToken ct = default) =>
            WithContextAsync(context =>
                All<PurchaseNote>(context)
                    .Include(pn => pn.Individual)
                    .Include(pn => pn.Warehouse)
                    .Include(pn => pn.LineItems)
                        .ThenInclude(li => li.Product)
                    .FirstOrDefaultAsync(pn => pn.NoteNumber == noteNumber, ct));

        public Task<IEnumerable<PurchaseNote>> GetByIndividualIdAsync(Guid individualId, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<PurchaseNote>)await All<PurchaseNote>(context)
                    .Where(pn => pn.IndividualId == individualId)
                    .Include(pn => pn.Individual)
                    .Include(pn => pn.Warehouse)
                    .Include(pn => pn.LineItems)
                        .ThenInclude(li => li.Product)
                    .OrderByDescending(pn => pn.PurchaseDate)
                    .ToListAsync(ct);
            });

        public Task<IEnumerable<PurchaseNote>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<PurchaseNote>)await All<PurchaseNote>(context)
                    .Where(pn => pn.PurchaseDate.Date >= startDate.Date &&
                                 pn.PurchaseDate.Date <= endDate.Date)
                    .Include(pn => pn.Individual)
                    .Include(pn => pn.Warehouse)
                    .Include(pn => pn.LineItems)
                        .ThenInclude(li => li.Product)
                    .OrderByDescending(pn => pn.PurchaseDate)
                    .ToListAsync(ct);
            });

        public Task<IEnumerable<PurchaseNote>> GetByStatusAsync(PurchaseNoteStatus status, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<PurchaseNote>)await All<PurchaseNote>(context)
                    .Where(pn => pn.Status == status)
                    .Include(pn => pn.Individual)
                    .Include(pn => pn.Warehouse)
                    .Include(pn => pn.LineItems)
                        .ThenInclude(li => li.Product)
                    .OrderByDescending(pn => pn.PurchaseDate)
                    .ToListAsync(ct);
            });

        public Task<IEnumerable<PurchaseNoteLine>> GetLineItemsByProductIdAsync(Guid productId, CancellationToken ct = default) =>
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
                    .ToListAsync(ct);
            });

        public Task<IEnumerable<PurchaseNoteLine>> GetLineItemsByProductIdAsync(
            Guid productId, Guid? warehouseId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                IQueryable<PurchaseNoteLine> q = All<PurchaseNoteLine>(context)
                    .Where(li => li.ProductId == productId &&
                                 li.PurchaseNote.DeletedOn == null &&
                                 li.PurchaseNote.Status != PurchaseNoteStatus.Cancelled);

                if (warehouseId.HasValue)
                    q = q.Where(li => li.PurchaseNote.WarehouseId == warehouseId.Value);
                if (dateFrom.HasValue)
                    q = q.Where(li => li.PurchaseNote.PurchaseDate >= dateFrom.Value);
                if (dateTo.HasValue)
                    q = q.Where(li => li.PurchaseNote.PurchaseDate <= dateTo.Value);

                return (IEnumerable<PurchaseNoteLine>)await q
                    .Include(li => li.PurchaseNote)
                        .ThenInclude(pn => pn.Individual)
                    .ToListAsync(ct);
            });

        public Task<IEnumerable<PurchaseNoteLine>> GetLineItemsByProductIdsAsync(
            List<Guid> productIds, Guid? warehouseId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                IQueryable<PurchaseNoteLine> q = All<PurchaseNoteLine>(context)
                    .Where(li => productIds.Contains(li.ProductId) &&
                                 li.PurchaseNote.DeletedOn == null &&
                                 li.PurchaseNote.Status != PurchaseNoteStatus.Cancelled);

                if (warehouseId.HasValue)
                    q = q.Where(li => li.PurchaseNote.WarehouseId == warehouseId.Value);
                if (dateFrom.HasValue)
                    q = q.Where(li => li.PurchaseNote.PurchaseDate >= dateFrom.Value);
                if (dateTo.HasValue)
                    q = q.Where(li => li.PurchaseNote.PurchaseDate <= dateTo.Value);

                return (IEnumerable<PurchaseNoteLine>)await q
                    .Include(li => li.Product)
                    .ToListAsync(ct);
            });

        public Task<PagedResult<PurchaseNoteLine>> GetPagedLineItemsByProductIdAsync(GetProductHistoryQuery query, CancellationToken ct = default) =>
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

                int totalCount = await q.CountAsync(ct);

                List<PurchaseNoteLine> items = await q
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync(ct);

                return new PagedResult<PurchaseNoteLine>
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = query.Page,
                    PageSize = query.PageSize
                };
            });

        public Task<string> GenerateNoteNumberAsync(CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                string? lastNoteNumber = await context.PurchaseNotes
                    .Where(pn => pn.NoteNumber.StartsWith("OB-"))
                    .OrderByDescending(pn => pn.NoteNumber)
                    .Select(pn => pn.NoteNumber)
                    .FirstOrDefaultAsync(ct);

                if (string.IsNullOrEmpty(lastNoteNumber))
                    return "OB-000001";

                string numberPart = lastNoteNumber.Replace("OB-", "");
                if (int.TryParse(numberPart, out int lastNumber))
                    return $"OB-{(lastNumber + 1):D6}";

                return "OB-000001";
            });

        public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) =>
            WithContextAsync(context =>
                All<PurchaseNote>(context).AnyAsync(pn => pn.Id == id, ct));

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
                PurchaseNote? tracked = await context.PurchaseNotes.FindAsync(purchaseNote.Id)
                    ?? throw new KeyNotFoundException($"Purchase note {purchaseNote.Id} not found");
                context.Entry(tracked).CurrentValues.SetValues(purchaseNote);

                foreach (PurchaseNoteLine li in purchaseNote.LineItems)
                {
                    if (li.Id == Guid.Empty)
                    {
                        li.PurchaseNoteId = purchaseNote.Id;
                        context.PurchaseNoteLines.Add(li);
                    }
                    else
                    {
                        PurchaseNoteLine? trackedLine = await context.PurchaseNoteLines.FindAsync(li.Id);
                        if (trackedLine is not null)
                            context.Entry(trackedLine).CurrentValues.SetValues(li);
                    }
                }

                await SaveAsync(context);
            });

        public Task<bool> DeleteAsync(Guid id) =>
            WithContextAsync(async context =>
            {
                PurchaseNote? purchaseNote = await context.PurchaseNotes
                    .Include(pn => pn.LineItems)
                    .FirstOrDefaultAsync(pn => pn.Id == id);
                if (purchaseNote == null)
                    return false;

                DateTime now = DateTime.UtcNow;
                purchaseNote.DeletedOn = now;
                foreach (PurchaseNoteLine line in purchaseNote.LineItems)
                    line.DeletedOn = now;

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
                q = q.Where(p => p.PurchaseDate >= query.DateFrom.Value.Date);

            if (query.DateTo.HasValue)
                q = q.Where(p => p.PurchaseDate < query.DateTo.Value.Date.AddDays(1));

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

            if (query.IndividualId.HasValue)
                q = q.Where(li => li.PurchaseNote.IndividualId == query.IndividualId.Value);

            // Purchase notes have no company party — return nothing when filtering by company
            if (query.CompanyId.HasValue)
                return q.Where(_ => false);

            if (query.DateFrom.HasValue)
                q = q.Where(li => li.PurchaseNote.PurchaseDate >= query.DateFrom.Value.Date);

            if (query.DateTo.HasValue)
                q = q.Where(li => li.PurchaseNote.PurchaseDate < query.DateTo.Value.Date.AddDays(1));

            return q;
        }

        // ── Dashboard targeted queries ────────────────────────────────────────────

        public Task<IEnumerable<PurchaseNote>> GetRecentAsync(int count, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<PurchaseNote>)await All<PurchaseNote>(context)
                    .Include(pn => pn.Individual)
                    .Include(pn => pn.Warehouse)
                    .OrderByDescending(pn => pn.PurchaseDate)
                    .Take(count)
                    .ToListAsync(ct);
            });

        public Task<IEnumerable<PurchaseNote>> GetByPurchaseDateAsync(DateTime date, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<PurchaseNote>)await All<PurchaseNote>(context)
                    .Where(pn => pn.PurchaseDate.Date == date.Date)
                    .Include(pn => pn.Individual)
                    .Include(pn => pn.Warehouse)
                    .OrderByDescending(pn => pn.PurchaseDate)
                    .ToListAsync(ct);
            });

        public Task<IEnumerable<PurchaseNote>> GetByPurchaseDateMonthAsync(int year, int month, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<PurchaseNote>)await All<PurchaseNote>(context)
                    .Where(pn => pn.PurchaseDate.Year == year && pn.PurchaseDate.Month == month)
                    .Include(pn => pn.Individual)
                    .Include(pn => pn.Warehouse)
                    .OrderByDescending(pn => pn.PurchaseDate)
                    .ToListAsync(ct);
            });

        public Task<(int unpaidCount, decimal unpaidAmount)> GetOutstandingPositionAsync(CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                var result = await All<PurchaseNote>(context)
                    .Where(pn => pn.Status == PurchaseNoteStatus.Pending)
                    .GroupBy(_ => 1)
                    .Select(g => new { Count = g.Count(), Amount = g.Sum(pn => pn.TotalAmount) })
                    .FirstOrDefaultAsync(ct);

                return (result?.Count ?? 0, result?.Amount ?? 0m);
            });

        public Task<IEnumerable<PartnerSummaryResult>> GetTopVendorsBySpendAsync(
            DateTime from, DateTime to, int topCount, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<PartnerSummaryResult>)await All<PurchaseNote>(context)
                    .Where(pn => pn.Status != PurchaseNoteStatus.Draft &&
                                 pn.Status != PurchaseNoteStatus.Cancelled &&
                                 pn.PurchaseDate >= from.Date &&
                                 pn.PurchaseDate < to.Date.AddDays(1))
                    .GroupBy(pn => new { pn.IndividualId, pn.Individual.FirstName, pn.Individual.LastName })
                    .Select(g => new PartnerSummaryResult
                    {
                        PartnerId = g.Key.IndividualId,
                        PartnerName = g.Key.FirstName + " " + g.Key.LastName,
                        Count = g.Count(),
                        Amount = g.Sum(pn => pn.TotalAmount)
                    })
                    .OrderByDescending(x => x.Amount)
                    .Take(topCount)
                    .ToListAsync(ct);
            });

        public Task<IEnumerable<PartnerSummaryResult>> GetUnpaidVendorSummariesAsync(CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<PartnerSummaryResult>)await All<PurchaseNote>(context)
                    .Where(pn => pn.Status == PurchaseNoteStatus.Pending)
                    .GroupBy(pn => new { pn.IndividualId, pn.Individual.FirstName, pn.Individual.LastName })
                    .Select(g => new PartnerSummaryResult
                    {
                        PartnerId = g.Key.IndividualId,
                        PartnerName = g.Key.FirstName + " " + g.Key.LastName,
                        Count = g.Count(),
                        Amount = g.Sum(pn => pn.TotalAmount)
                    })
                    .OrderByDescending(x => x.Amount)
                    .Take(5)
                    .ToListAsync(ct);
            });

        public Task<IEnumerable<ProductMovementResult>> GetProductPurchasesByWarehouseAsync(
            Guid warehouseId, DateTime from, DateTime to, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                var results = await All<PurchaseNoteLine>(context)
                    .Where(li => li.PurchaseNote.WarehouseId == warehouseId &&
                                 li.PurchaseNote.DeletedOn == null &&
                                 li.PurchaseNote.Status != PurchaseNoteStatus.Cancelled &&
                                 li.PurchaseNote.PurchaseDate.Date >= from.Date &&
                                 li.PurchaseNote.PurchaseDate.Date <= to.Date)
                    .GroupBy(li => li.ProductId)
                    .Select(g => new
                    {
                        ProductId = g.Key,
                        Quantity = (decimal)g.Sum(li => li.Quantity),
                        TotalAmount = g.Sum(li => li.Quantity * li.UnitPrice)
                    })
                    .ToListAsync(ct);

                return results
                    .Select(r => new ProductMovementResult
                    {
                        ProductId = r.ProductId,
                        Quantity = r.Quantity,
                        TotalAmount = r.TotalAmount
                    });
            });

        public Task<IEnumerable<ProductMovementWithNameResult>> GetTopProductPurchasesByWarehouseAsync(
            Guid warehouseId, DateTime from, DateTime to, int top, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                var results = await All<PurchaseNoteLine>(context)
                    .Where(li => li.PurchaseNote.WarehouseId == warehouseId &&
                                 li.PurchaseNote.DeletedOn == null &&
                                 li.PurchaseNote.Status != PurchaseNoteStatus.Cancelled &&
                                 li.PurchaseNote.PurchaseDate.Date >= from.Date &&
                                 li.PurchaseNote.PurchaseDate.Date <= to.Date)
                    .GroupBy(li => new { li.ProductId, li.Product.Code, li.Product.Name, li.Product.Unit })
                    .Select(g => new
                    {
                        g.Key.ProductId,
                        g.Key.Code,
                        g.Key.Name,
                        g.Key.Unit,
                        Quantity = (decimal)g.Sum(li => li.Quantity),
                        TotalAmount = g.Sum(li => li.Quantity * li.UnitPrice)
                    })
                    .OrderByDescending(r => r.TotalAmount)
                    .Take(top)
                    .ToListAsync(ct);

                return (IEnumerable<ProductMovementWithNameResult>)results.Select(r => new ProductMovementWithNameResult
                {
                    ProductId = r.ProductId,
                    ProductCode = r.Code,
                    ProductName = r.Name,
                    Unit = r.Unit,
                    Quantity = r.Quantity,
                    TotalAmount = r.TotalAmount
                }).ToList();
            });

        public Task<IndividualAnalyticsResult> GetIndividualAnalyticsDataAsync(Guid individualId, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                IQueryable<PurchaseNote> base_q = All<PurchaseNote>(context).Where(pn => pn.IndividualId == individualId);

                // ── 1. Grouped stats by Status ────────────────────────────────
                var statRows = await base_q
                    .GroupBy(pn => pn.Status)
                    .Select(g => new IndividualNoteStatRow
                    {
                        Status      = g.Key,
                        Count       = g.Count(),
                        TotalAmount = g.Sum(pn => pn.TotalAmount)
                    })
                    .ToListAsync(ct);

                // ── 2. Most purchased product (non-cancelled) ─────────────────
                var mostPurchased = await All<PurchaseNoteLine>(context)
                    .Where(li => li.PurchaseNote.IndividualId == individualId &&
                                 li.PurchaseNote.Status != PurchaseNoteStatus.Cancelled)
                    .GroupBy(li => new { li.ProductId, li.Product.Name, li.Product.Unit })
                    .Select(g => new
                    {
                        g.Key.Name,
                        g.Key.Unit,
                        TotalQuantity = g.Sum(li => li.Quantity)
                    })
                    .OrderByDescending(x => x.TotalQuantity)
                    .FirstOrDefaultAsync(ct);

                // ── 3. First / last purchase date ─────────────────────────────
                var dates = await base_q
                    .GroupBy(_ => 1)
                    .Select(g => new { First = g.Min(pn => pn.PurchaseDate), Last = g.Max(pn => pn.PurchaseDate) })
                    .FirstOrDefaultAsync(ct);

                // ── 4. Recent 5 purchase notes ────────────────────────────────
                var recent = await base_q
                    .OrderByDescending(pn => pn.PurchaseDate)
                    .Take(5)
                    .Select(pn => new IndividualRecentNoteRow
                    {
                        Id           = pn.Id,
                        NoteNumber   = pn.NoteNumber,
                        PurchaseDate = pn.PurchaseDate,
                        TotalAmount  = pn.TotalAmount,
                        Status       = pn.Status
                    })
                    .ToListAsync(ct);

                return new IndividualAnalyticsResult
                {
                    StatRows                    = statRows,
                    MostPurchasedProductName    = mostPurchased?.Name,
                    MostPurchasedProductQuantity = mostPurchased?.TotalQuantity ?? 0,
                    MostPurchasedProductUnit    = mostPurchased?.Unit,
                    FirstPurchaseDate           = dates?.First,
                    LastPurchaseDate            = dates?.Last,
                    RecentNotes                 = recent
                };
            });

        // ── Home dashboard aggregates ─────────────────────────────────────────────

        public Task<DayPurchaseNoteSummaryResult> GetDayPaidSummaryAsync(DateTime date, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                DateTime day = date.Date;
                var result = await All<PurchaseNote>(context)
                    .Where(pn => pn.PaidDate.HasValue && pn.PaidDate.Value.Date == day)
                    .GroupBy(_ => 1)
                    .Select(g => new DayPurchaseNoteSummaryResult
                    {
                        Count = g.Count(),
                        Amount = g.Sum(pn => pn.TotalAmount)
                    })
                    .FirstOrDefaultAsync(ct);
                return result ?? new DayPurchaseNoteSummaryResult();
            });

        public Task<IEnumerable<PurchaseNote>> GetTopUnpaidAsync(
            Guid? warehouseId, int top, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                IQueryable<PurchaseNote> q = All<PurchaseNote>(context)
                    .Where(pn => pn.Status == PurchaseNoteStatus.Pending)
                    .Include(pn => pn.Individual)
                    .Include(pn => pn.Warehouse);

                if (warehouseId.HasValue)
                    q = q.Where(pn => pn.WarehouseId == warehouseId.Value);

                return (IEnumerable<PurchaseNote>)await q
                    .OrderBy(pn => pn.PurchaseDate)
                    .Take(top)
                    .ToListAsync(ct);
            });

        public Task<DayPurchaseNoteSummaryResult> GetDayIssuedSummaryAsync(DateTime date, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                DateTime day = date.Date;
                var result = await All<PurchaseNote>(context)
                    .Where(pn => pn.PurchaseDate.Date == day &&
                                 pn.Status != PurchaseNoteStatus.Cancelled)
                    .GroupBy(_ => 1)
                    .Select(g => new DayPurchaseNoteSummaryResult
                    {
                        Count = g.Count(),
                        Amount = g.Sum(pn => pn.TotalAmount)
                    })
                    .FirstOrDefaultAsync(ct);
                return result ?? new DayPurchaseNoteSummaryResult();
            });

        public Task<DayPurchaseNoteSummaryResult> GetMonthIssuedSummaryAsync(int year, int month, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                var result = await All<PurchaseNote>(context)
                    .Where(pn => pn.PurchaseDate.Year == year &&
                                 pn.PurchaseDate.Month == month &&
                                 pn.Status != PurchaseNoteStatus.Cancelled)
                    .GroupBy(_ => 1)
                    .Select(g => new DayPurchaseNoteSummaryResult
                    {
                        Count = g.Count(),
                        Amount = g.Sum(pn => pn.TotalAmount)
                    })
                    .FirstOrDefaultAsync(ct);
                return result ?? new DayPurchaseNoteSummaryResult();
            });

        public Task<DayPurchaseNoteSummaryResult> GetYearIssuedSummaryAsync(int year, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                var result = await All<PurchaseNote>(context)
                    .Where(pn => pn.PurchaseDate.Year == year &&
                                 pn.Status != PurchaseNoteStatus.Cancelled)
                    .GroupBy(_ => 1)
                    .Select(g => new DayPurchaseNoteSummaryResult
                    {
                        Count = g.Count(),
                        Amount = g.Sum(pn => pn.TotalAmount)
                    })
                    .FirstOrDefaultAsync(ct);
                return result ?? new DayPurchaseNoteSummaryResult();
            });

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