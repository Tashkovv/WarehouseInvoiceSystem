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

    public class InvoiceRepository(IDbContextFactory<ApplicationDbContext> factory)
        : BaseRepository(factory), IInvoiceRepository
    {
        public Task<IEnumerable<Invoice>> GetAllAsync(CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<Invoice>)await All<Invoice>(context)
                    .Include(i => i.Company)
                    .Include(i => i.Warehouse)
                    .Include(i => i.LineItems)
                        .ThenInclude(li => li.Product)
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync(ct);
            });

        public Task<PagedResult<Invoice>> GetPagedAsync(GetInvoicesQuery query, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                IQueryable<Invoice> q = ApplyFilters(
                    All<Invoice>(context)
                        .Include(i => i.Company)
                        .Include(i => i.Warehouse),
                    query);

                q = ApplySort(q, query.SortBy, query.SortAscending);

                int totalCount = await q.CountAsync(ct);

                List<Invoice> items = await q
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync(ct);

                return new PagedResult<Invoice>
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = query.Page,
                    PageSize = query.PageSize
                };
            });

        public Task<IEnumerable<Invoice>> GetByCompanyIdAsync(Guid companyId, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<Invoice>)await All<Invoice>(context)
                    .Where(i => i.CompanyId == companyId)
                    .Include(i => i.Company)
                    .Include(i => i.Warehouse)
                    .Include(i => i.LineItems)
                        .ThenInclude(li => li.Product)
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync(ct);
            });

        public Task<IEnumerable<Invoice>> GetByTypeAsync(InvoiceType type, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<Invoice>)await All<Invoice>(context)
                    .Where(i => i.Type == type)
                    .Include(i => i.Company)
                    .Include(i => i.Warehouse)
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync(ct);
            });

        public Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<Invoice>)await All<Invoice>(context)
                    .Where(i => i.Status == status)
                    .Include(i => i.Company)
                    .Include(i => i.Warehouse)
                    .Include(i => i.LineItems)
                        .ThenInclude(li => li.Product)
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync(ct);
            });

        public Task<IEnumerable<Invoice>> GetOverdueInvoicesAsync(CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                DateTime today = DateTime.UtcNow.Date;
                return (IEnumerable<Invoice>)await All<Invoice>(context)
                    .Where(i => i.DueDate < today &&
                                i.Status != InvoiceStatus.Paid &&
                                i.Status != InvoiceStatus.Cancelled)
                    .Include(i => i.Company)
                    .Include(i => i.Warehouse)
                    .OrderBy(i => i.DueDate)
                    .ToListAsync(ct);
            });

        public Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            WithContextAsync(context =>
                All<Invoice>(context)
                    .Include(i => i.Company)
                    .Include(i => i.LineItems)
                        .ThenInclude(li => li.Product)
                    .Include(i => i.Payments)
                    .Include(i => i.Warehouse)
                    .FirstOrDefaultAsync(i => i.Id == id, ct));

        public Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken ct = default) =>
            WithContextAsync(context =>
                All<Invoice>(context)
                    .Include(i => i.Company)
                    .Include(i => i.Warehouse)
                    .Include(i => i.LineItems)
                        .ThenInclude(li => li.Product)
                    .Include(i => i.Payments)
                    .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber, ct));

        public Task<IEnumerable<InvoiceLine>> GetLineItemsByProductIdAsync(Guid productId, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<InvoiceLine>)await All<InvoiceLine>(context)
                    .Where(li => li.ProductId == productId &&
                                 li.Invoice.DeletedOn == null &&
                                 li.Invoice.Status != InvoiceStatus.Cancelled)
                    .Include(li => li.Invoice)
                        .ThenInclude(i => i.Company)
                    .Include(li => li.Invoice)
                        .ThenInclude(i => i.Warehouse)
                    .OrderByDescending(li => li.Invoice.IssueDate)
                    .ToListAsync(ct);
            });

        public Task<PagedResult<InvoiceLine>> GetPagedLineItemsByProductIdAsync(GetProductHistoryQuery query, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                IQueryable<InvoiceLine> q = ApplyLineItemFilters(
                    All<InvoiceLine>(context)
                        .Include(li => li.Invoice)
                            .ThenInclude(i => i.Company)
                        .Include(li => li.Invoice)
                            .ThenInclude(i => i.Warehouse),
                    query);

                q = q.OrderByDescending(li => li.Invoice.IssueDate);

                int totalCount = await q.CountAsync(ct);

                List<InvoiceLine> items = await q
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync(ct);

                return new PagedResult<InvoiceLine>
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = query.Page,
                    PageSize = query.PageSize
                };
            });

        public Task<Guid> CreateAsync(Invoice invoice) =>
            WithContextAsync(async context =>
            {
                invoice.CreatedAt = DateTime.UtcNow;
                context.Invoices.Add(invoice);
                await SaveAsync(context);
                return invoice.Id;
            });

        public Task<Invoice> UpdateAsync(Invoice invoice) =>
            WithContextAsync(async context =>
            {
                Invoice? tracked = await context.Invoices.FindAsync(invoice.Id)
                    ?? throw new KeyNotFoundException($"Invoice {invoice.Id} not found");
                context.Entry(tracked).CurrentValues.SetValues(invoice);

                foreach (InvoiceLine li in invoice.LineItems)
                {
                    if (li.Id == Guid.Empty)
                    {
                        li.InvoiceId = invoice.Id;
                        context.InvoiceLines.Add(li);
                    }
                    else
                    {
                        InvoiceLine? trackedLine = await context.InvoiceLines.FindAsync(li.Id);
                        if (trackedLine is not null)
                            context.Entry(trackedLine).CurrentValues.SetValues(li);
                    }
                }

                await SaveAsync(context);

                return (await All<Invoice>(context)
                    .Include(i => i.Company)
                    .Include(i => i.Warehouse)
                    .Include(i => i.LineItems)
                        .ThenInclude(li => li.Product)
                    .Include(i => i.Payments)
                    .FirstOrDefaultAsync(i => i.Id == invoice.Id))!;
            });

        public Task<bool> DeleteAsync(Guid id) =>
            WithContextAsync(async context =>
            {
                Invoice? invoice = await context.Invoices
                    .Include(i => i.LineItems)
                    .FirstOrDefaultAsync(i => i.Id == id);
                if (invoice == null)
                    return false;

                DateTime now = DateTime.UtcNow;
                invoice.DeletedOn = now;
                foreach (InvoiceLine line in invoice.LineItems)
                    line.DeletedOn = now;

                await SaveAsync(context);
                return true;
            });

        public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) =>
            WithContextAsync(context =>
                All<Invoice>(context).AnyAsync(i => i.Id == id, ct));

        public Task<string> GenerateInvoiceNumberAsync(InvoiceType type, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                string sequenceName = type == InvoiceType.Receivable
                    ? "invoice_number_seq"
                    : "bill_number_seq";

                string prefix = type == InvoiceType.Receivable ? "INV" : "BILL";
                int year = DateTime.UtcNow.Year;
                int month = DateTime.UtcNow.Month;

                long nextNumber = await context.Database
                    .SqlQueryRaw<long>($"SELECT nextval('{sequenceName}') AS \"Value\"")
                    .FirstAsync(ct);

                return $"{prefix}-{year:D4}{month:D2}{nextNumber:D4}";
            });

        public Task<(int total, int paid, int unpaid, int overdue)> GetPayableInvoiceCountsAsync(CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                DateTime today = DateTime.UtcNow.Date;
                IQueryable<Invoice> invoices = All<Invoice>(context);

                int total = await invoices.CountAsync(ct);
                int paid = await invoices.CountAsync(i => i.Status == InvoiceStatus.Paid, ct);
                int unpaid = await invoices.CountAsync(i => i.Type == InvoiceType.Payable &&
                                                              i.Status != InvoiceStatus.Paid &&
                                                              i.Status != InvoiceStatus.Cancelled, ct);
                int overdue = await invoices.CountAsync(i => i.DueDate < today &&
                                                              i.Status != InvoiceStatus.Paid &&
                                                              i.Status != InvoiceStatus.Cancelled, ct);

                return (total, paid, unpaid, overdue);
            });

        public Task<(decimal totalAmount, decimal totalPaid, decimal totalDue)> GetPayableInvoiceTotalsAsync(CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                IQueryable<Invoice> invoices = All<Invoice>(context)
                    .Where(i => i.Status != InvoiceStatus.Cancelled &&
                                i.Type == InvoiceType.Payable);

                decimal totalAmount = await invoices.SumAsync(i => i.TotalAmount, ct);
                decimal totalPaid = await invoices.SumAsync(i => i.AmountPaid, ct);
                decimal totalDue = await invoices.SumAsync(i => i.TotalAmount - i.AmountPaid, ct);

                return (totalAmount, totalPaid, totalDue);
            });

        // ── Dashboard targeted queries ────────────────────────────────────────────

        public Task<IEnumerable<Invoice>> GetRecentAsync(int count, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<Invoice>)await All<Invoice>(context)
                    .Include(i => i.Company)
                    .Include(i => i.Warehouse)
                    .OrderByDescending(i => i.CreatedAt)
                    .Take(count)
                    .ToListAsync(ct);
            });

        public Task<IEnumerable<Invoice>> GetByIssueDateAsync(DateTime date, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<Invoice>)await All<Invoice>(context)
                    .Where(i => i.IssueDate.Date == date.Date)
                    .Include(i => i.Company)
                    .Include(i => i.Warehouse)
                    .OrderByDescending(i => i.IssueDate)
                    .ToListAsync(ct);
            });

        public Task<IEnumerable<Invoice>> GetByIssueDateMonthAsync(int year, int month, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<Invoice>)await All<Invoice>(context)
                    .Where(i => i.IssueDate.Year == year && i.IssueDate.Month == month)
                    .Include(i => i.Company)
                    .Include(i => i.Warehouse)
                    .OrderByDescending(i => i.IssueDate)
                    .ToListAsync(ct);
            });

        public Task<InvoiceOutstandingResult> GetOutstandingPositionAsync(CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                var stats = await All<Invoice>(context)
                    .Where(i => i.Status != InvoiceStatus.Draft)
                    .GroupBy(i => new { i.Type, i.Status })
                    .Select(g => new
                    {
                        g.Key.Type,
                        g.Key.Status,
                        Count = g.Count(),
                        AmountDue = g.Sum(i => i.TotalAmount - i.AmountPaid)
                    })
                    .ToListAsync(ct);

                return new InvoiceOutstandingResult
                {
                    ReceivableCount = stats
                        .Where(s => s.Type == InvoiceType.Receivable && s.Status != InvoiceStatus.Paid && s.Status != InvoiceStatus.Cancelled)
                        .Sum(s => s.Count),
                    ReceivableAmount = stats
                        .Where(s => s.Type == InvoiceType.Receivable && s.Status != InvoiceStatus.Paid && s.Status != InvoiceStatus.Cancelled)
                        .Sum(s => s.AmountDue),
                    OverdueReceivableCount = stats
                        .Where(s => s.Type == InvoiceType.Receivable && s.Status == InvoiceStatus.Overdue)
                        .Sum(s => s.Count),
                    OverdueReceivableAmount = stats
                        .Where(s => s.Type == InvoiceType.Receivable && s.Status == InvoiceStatus.Overdue)
                        .Sum(s => s.AmountDue),
                    PayableCount = stats
                        .Where(s => s.Type == InvoiceType.Payable && s.Status != InvoiceStatus.Paid && s.Status != InvoiceStatus.Cancelled)
                        .Sum(s => s.Count),
                    PayableAmount = stats
                        .Where(s => s.Type == InvoiceType.Payable && s.Status != InvoiceStatus.Paid && s.Status != InvoiceStatus.Cancelled)
                        .Sum(s => s.AmountDue),
                    TotalOverdueCount = stats
                        .Where(s => s.Status == InvoiceStatus.Overdue)
                        .Sum(s => s.Count)
                };
            });

        public Task<IEnumerable<PartnerSummaryResult>> GetTopClientsByRevenueAsync(
            DateTime from, DateTime to, int topCount, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<PartnerSummaryResult>)await All<Invoice>(context)
                    .Where(i => i.Type == InvoiceType.Receivable &&
                                i.Status != InvoiceStatus.Draft &&
                                i.Status != InvoiceStatus.Cancelled &&
                                i.IssueDate.Date >= from.Date &&
                                i.IssueDate.Date <= to.Date)
                    .GroupBy(i => new { i.CompanyId, i.Company.Name })
                    .Select(g => new PartnerSummaryResult
                    {
                        PartnerId = g.Key.CompanyId,
                        PartnerName = g.Key.Name,
                        Count = g.Count(),
                        Amount = g.Sum(i => i.TotalAmount)
                    })
                    .OrderByDescending(x => x.Amount)
                    .Take(topCount)
                    .ToListAsync(ct);
            });

        public Task<IEnumerable<PartnerSummaryResult>> GetTopPayableVendorsBySpendAsync(
            DateTime from, DateTime to, int topCount, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<PartnerSummaryResult>)await All<Invoice>(context)
                    .Where(i => i.Type == InvoiceType.Payable &&
                                i.Status != InvoiceStatus.Draft &&
                                i.Status != InvoiceStatus.Cancelled &&
                                i.IssueDate.Date >= from.Date &&
                                i.IssueDate.Date <= to.Date)
                    .GroupBy(i => new { i.CompanyId, i.Company.Name })
                    .Select(g => new PartnerSummaryResult
                    {
                        PartnerId = g.Key.CompanyId,
                        PartnerName = g.Key.Name,
                        Count = g.Count(),
                        Amount = g.Sum(i => i.TotalAmount)
                    })
                    .OrderByDescending(x => x.Amount)
                    .Take(topCount)
                    .ToListAsync(ct);
            });

        public Task<IEnumerable<PartnerSummaryResult>> GetOverdueClientSummariesAsync(CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<PartnerSummaryResult>)await All<Invoice>(context)
                    .Where(i => i.Type == InvoiceType.Receivable && i.Status == InvoiceStatus.Overdue)
                    .GroupBy(i => new { i.CompanyId, i.Company.Name })
                    .Select(g => new PartnerSummaryResult
                    {
                        PartnerId = g.Key.CompanyId,
                        PartnerName = g.Key.Name,
                        Count = g.Count(),
                        Amount = g.Sum(i => i.TotalAmount - i.AmountPaid)
                    })
                    .OrderByDescending(x => x.Amount)
                    .ToListAsync(ct);
            });

        public Task<IEnumerable<PartnerSummaryResult>> GetUnpaidPayableCompanySummariesAsync(CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<PartnerSummaryResult>)await All<Invoice>(context)
                    .Where(i => i.Type == InvoiceType.Payable &&
                                (i.Status == InvoiceStatus.Sent ||
                                 i.Status == InvoiceStatus.PartiallyPaid ||
                                 i.Status == InvoiceStatus.Overdue))
                    .GroupBy(i => new { i.CompanyId, i.Company.Name })
                    .Select(g => new PartnerSummaryResult
                    {
                        PartnerId = g.Key.CompanyId,
                        PartnerName = g.Key.Name,
                        Count = g.Count(),
                        Amount = g.Sum(i => i.TotalAmount - i.AmountPaid)
                    })
                    .OrderByDescending(x => x.Amount)
                    .ToListAsync(ct);
            });

        public Task<IEnumerable<ProductMovementResult>> GetProductMovementByWarehouseAsync(
            Guid warehouseId, InvoiceType type, DateTime from, DateTime to, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                var results = await All<InvoiceLine>(context)
                    .Where(li => li.Invoice.WarehouseId == warehouseId &&
                                 li.Invoice.Type == type &&
                                 li.Invoice.DeletedOn == null &&
                                 li.Invoice.Status != InvoiceStatus.Cancelled &&
                                 li.Invoice.IssueDate.Date >= from.Date &&
                                 li.Invoice.IssueDate.Date <= to.Date)
                    .GroupBy(li => li.ProductId)
                    .Select(g => new
                    {
                        ProductId = g.Key,
                        Quantity = (decimal)g.Sum(li => li.Quantity),
                        TotalAmount = g.Sum(li => li.Quantity * li.UnitPrice * (1 + li.TaxRate / 100))
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

        public Task<CompanyAnalyticsResult> GetCompanyAnalyticsDataAsync(Guid companyId, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                IQueryable<Invoice> base_q = All<Invoice>(context).Where(i => i.CompanyId == companyId);

                // ── 1. Grouped stats by (Type, Status) ───────────────────────────
                var statRows = await base_q
                    .GroupBy(i => new { i.Type, i.Status })
                    .Select(g => new CompanyInvoiceStatRow
                    {
                        Type        = g.Key.Type,
                        Status      = g.Key.Status,
                        Count       = g.Count(),
                        TotalAmount = g.Sum(i => i.TotalAmount),
                        AmountPaid  = g.Sum(i => i.AmountPaid),
                        AmountDue   = g.Sum(i => i.TotalAmount - i.AmountPaid)
                    })
                    .ToListAsync(ct);

                // ── 2. Most traded product (non-cancelled invoices) ───────────────
                var mostTraded = await All<InvoiceLine>(context)
                    .Where(li => li.Invoice.CompanyId == companyId &&
                                 li.Invoice.Status != InvoiceStatus.Cancelled)
                    .GroupBy(li => new { li.ProductId, li.Product.Name, li.Product.Unit })
                    .Select(g => new
                    {
                        g.Key.Name,
                        g.Key.Unit,
                        TotalQuantity = g.Sum(li => li.Quantity)
                    })
                    .OrderByDescending(x => x.TotalQuantity)
                    .FirstOrDefaultAsync(ct);

                // ── 3. First / last invoice date (non-cancelled) ──────────────────
                var dates = await base_q
                    .Where(i => i.Status != InvoiceStatus.Cancelled)
                    .GroupBy(_ => 1)
                    .Select(g => new { First = g.Min(i => i.IssueDate), Last = g.Max(i => i.IssueDate) })
                    .FirstOrDefaultAsync(ct);

                // ── 4. Recent 5 invoices ──────────────────────────────────────────
                var recent = await base_q
                    .OrderByDescending(i => i.IssueDate)
                    .Take(5)
                    .Select(i => new CompanyRecentInvoiceRow
                    {
                        Id            = i.Id,
                        InvoiceNumber = i.InvoiceNumber,
                        Type          = i.Type,
                        Status        = i.Status,
                        IssueDate     = i.IssueDate,
                        DueDate       = i.DueDate,
                        TotalAmount   = i.TotalAmount,
                        AmountDue     = i.TotalAmount - i.AmountPaid
                    })
                    .ToListAsync(ct);

                return new CompanyAnalyticsResult
                {
                    StatRows                 = statRows,
                    MostTradedProductName    = mostTraded?.Name,
                    MostTradedProductQuantity = mostTraded?.TotalQuantity ?? 0,
                    MostTradedProductUnit    = mostTraded?.Unit,
                    FirstInvoiceDate         = dates?.First,
                    LastInvoiceDate          = dates?.Last,
                    RecentInvoices           = recent
                };
            });

        public Task<PagedResult<ProductPurchaseHistoryView>> GetPagedPurchasedHistoryAsync(
            GetProductHistoryQuery query, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                IQueryable<ProductPurchaseHistoryView> q = context.Set<ProductPurchaseHistoryView>()
                    .Where(v => v.ProductId == query.ProductId);

                if (query.WarehouseId.HasValue)
                    q = q.Where(v => v.WarehouseId == query.WarehouseId.Value);

                if (query.CompanyId.HasValue)
                    q = q.Where(v => v.CompanyId == query.CompanyId.Value);

                if (query.IndividualId.HasValue)
                    q = q.Where(v => v.IndividualId == query.IndividualId.Value);

                if (query.DateFrom.HasValue)
                    q = q.Where(v => v.Date >= query.DateFrom.Value.Date);

                if (query.DateTo.HasValue)
                    q = q.Where(v => v.Date < query.DateTo.Value.Date.AddDays(1));

                q = q.OrderByDescending(v => v.Date);

                int totalCount = await q.CountAsync(ct);

                List<ProductPurchaseHistoryView> items = await q
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync(ct);

                return new PagedResult<ProductPurchaseHistoryView>
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = query.Page,
                    PageSize = query.PageSize
                };
            });

        private static IQueryable<Invoice> ApplyFilters(IQueryable<Invoice> q, GetInvoicesQuery query)
        {
            if (query.Statuses is { Count: > 0 })
                q = q.Where(i => query.Statuses.Contains(i.Status));
            else if (query.Status.HasValue)
                q = q.Where(i => i.Status == query.Status.Value);

            if (query.Type.HasValue)
                q = q.Where(i => i.Type == query.Type.Value);

            if (!string.IsNullOrWhiteSpace(query.CompanyName))
                q = q.Where(i => i.Company.Name == query.CompanyName);

            if (query.CompanyId.HasValue)
                q = q.Where(i => i.CompanyId == query.CompanyId.Value);

            if (query.AmountMin.HasValue)
                q = q.Where(i => i.TotalAmount >= query.AmountMin.Value);

            if (query.AmountMax.HasValue)
                q = q.Where(i => i.TotalAmount <= query.AmountMax.Value);

            if (query.IssueDateFrom.HasValue)
                q = q.Where(i => i.IssueDate.Date >= query.IssueDateFrom.Value.Date);

            if (query.IssueDateTo.HasValue)
                q = q.Where(i => i.IssueDate.Date < query.IssueDateTo.Value.Date.AddDays(1));

            if (query.DueDateFrom.HasValue)
                q = q.Where(i => i.DueDate.Date >= query.DueDateFrom.Value.Date);

            if (query.DueDateTo.HasValue)
                q = q.Where(i => i.DueDate.Date < query.DueDateTo.Value.Date.AddDays(1));

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                string search = $"%{query.Search}%";
                q = q.Where(i =>
                    EF.Functions.ILike(i.InvoiceNumber, search) ||
                    EF.Functions.ILike(i.Company.Name, search));
            }

            return q;
        }

        private static IQueryable<InvoiceLine> ApplyLineItemFilters(IQueryable<InvoiceLine> q, GetProductHistoryQuery query)
        {
            InvoiceType invoiceType = query.Purchased ? InvoiceType.Payable : InvoiceType.Receivable;

            q = q.Where(li => li.ProductId == query.ProductId &&
                               li.Invoice.DeletedOn == null &&
                               li.Invoice.Status != InvoiceStatus.Cancelled &&
                               li.Invoice.Type == invoiceType);

            if (query.WarehouseId.HasValue)
                q = q.Where(li => li.Invoice.WarehouseId == query.WarehouseId.Value);

            if (query.CompanyId.HasValue)
                q = q.Where(li => li.Invoice.CompanyId == query.CompanyId.Value);

            // Invoices have no individual party — return nothing when filtering by individual
            if (query.IndividualId.HasValue)
                return q.Where(_ => false);

            if (query.DateFrom.HasValue)
                q = q.Where(li => li.Invoice.IssueDate >= query.DateFrom.Value.Date);

            if (query.DateTo.HasValue)
                q = q.Where(li => li.Invoice.IssueDate < query.DateTo.Value.Date.AddDays(1));

            return q;
        }

        private static IQueryable<Invoice> ApplySort(IQueryable<Invoice> q, string? sortBy, bool ascending)
            => sortBy switch
            {
                "InvoiceNumber" => ascending ? q.OrderBy(i => i.InvoiceNumber) : q.OrderByDescending(i => i.InvoiceNumber),
                "CompanyName" => ascending ? q.OrderBy(i => i.Company.Name) : q.OrderByDescending(i => i.Company.Name),
                "IssueDate" => ascending ? q.OrderBy(i => i.IssueDate) : q.OrderByDescending(i => i.IssueDate),
                "DueDate" => ascending ? q.OrderBy(i => i.DueDate) : q.OrderByDescending(i => i.DueDate),
                "TotalAmount" => ascending ? q.OrderBy(i => i.TotalAmount) : q.OrderByDescending(i => i.TotalAmount),
                _ => q.OrderByDescending(i => i.CreatedAt)
            };
    }
}