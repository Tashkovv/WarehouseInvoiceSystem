namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    public class PaymentRepository(IDbContextFactory<ApplicationDbContext> factory)
        : BaseRepository(factory), IPaymentRepository
    {
        public Task<IEnumerable<Payment>> GetAllAsync() =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<Payment>)await All<Payment>(context)
                    .Include(p => p.Invoice)
                        .ThenInclude(i => i.Company)
                    .OrderByDescending(p => p.PaymentDate)
                    .ToListAsync();
            });

        public Task<PagedResult<Payment>> GetPagedAsync(GetPaymentsQuery query) =>
            WithContextAsync(async context =>
            {
                IQueryable<Payment> q = ApplyFilters(
                    All<Payment>(context)
                        .Include(p => p.Invoice)
                            .ThenInclude(i => i.Company),
                    query);

                q = ApplySort(q, query.SortBy, query.SortAscending);

                int totalCount = await q.CountAsync();

                List<Payment> items = await q
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync();

                return new PagedResult<Payment>
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = query.Page,
                    PageSize = query.PageSize
                };
            });

        public Task<IEnumerable<Payment>> GetByInvoiceIdAsync(Guid invoiceId) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<Payment>)await All<Payment>(context)
                    .Where(p => p.InvoiceId == invoiceId)
                    .Include(p => p.Invoice)
                    .OrderByDescending(p => p.PaymentDate)
                    .ToListAsync();
            });

        public Task<Payment?> GetByIdAsync(Guid id) =>
            WithContextAsync(context =>
                All<Payment>(context)
                    .Include(p => p.Invoice)
                        .ThenInclude(i => i.Company)
                    .FirstOrDefaultAsync(p => p.Id == id));

        public Task CreateAsync(Payment payment) =>
            WithContextAsync(async context =>
            {
                payment.CreatedAt = DateTime.UtcNow;
                context.Payments.Add(payment);
                await SaveAsync(context);
            });

        public Task UpdateAsync(Payment payment) =>
            WithContextAsync(async context =>
            {
                context.Payments.Update(payment);
                await SaveAsync(context);
            });

        public Task<bool> DeleteAsync(Guid id) =>
            WithContextAsync(async context =>
            {
                Payment? payment = await context.Payments.FirstOrDefaultAsync(p => p.Id == id);
                if (payment == null)
                    return false;

                payment.DeletedOn = DateTime.UtcNow;
                await SaveAsync(context);
                return true;
            });

        public Task<decimal> GetTotalPaymentsByInvoiceAsync(Guid invoiceId) =>
            WithContextAsync(context =>
                All<Payment>(context)
                    .Where(p => p.InvoiceId == invoiceId)
                    .SumAsync(p => p.Amount));

        private static IQueryable<Payment> ApplyFilters(IQueryable<Payment> q, GetPaymentsQuery query)
        {
            if (query.InvoiceId.HasValue)
                q = q.Where(p => p.InvoiceId == query.InvoiceId.Value);

            if (query.PaymentMethod.HasValue)
                q = q.Where(p => p.PaymentMethod == query.PaymentMethod.Value);

            if (query.DateFrom.HasValue)
                q = q.Where(p => p.PaymentDate >= query.DateFrom.Value);

            if (query.DateTo.HasValue)
                q = q.Where(p => p.PaymentDate <= query.DateTo.Value);

            if (query.AmountMin.HasValue)
                q = q.Where(p => p.Amount >= query.AmountMin.Value);

            if (query.AmountMax.HasValue)
                q = q.Where(p => p.Amount <= query.AmountMax.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                string search = $"%{query.Search}%";
                q = q.Where(p =>
                    (p.ReferenceNumber != null && EF.Functions.ILike(p.ReferenceNumber, search)) ||
                    EF.Functions.ILike(p.Invoice.InvoiceNumber, search) ||
                    EF.Functions.ILike(p.Invoice.Company.Name, search));
            }

            return q;
        }

        private static IQueryable<Payment> ApplySort(IQueryable<Payment> q, string? sortBy, bool ascending)
            => sortBy switch
            {
                "PaymentDate" => ascending ? q.OrderBy(p => p.PaymentDate) : q.OrderByDescending(p => p.PaymentDate),
                "InvoiceNumber" => ascending ? q.OrderBy(p => p.Invoice.InvoiceNumber) : q.OrderByDescending(p => p.Invoice.InvoiceNumber),
                "Amount" => ascending ? q.OrderBy(p => p.Amount) : q.OrderByDescending(p => p.Amount),
                "ReferenceNumber" => ascending ? q.OrderBy(p => p.ReferenceNumber) : q.OrderByDescending(p => p.ReferenceNumber),
                "CompanyName" => ascending ? q.OrderBy(p => p.Invoice.Company.Name) : q.OrderByDescending(p => p.Invoice.Company.Name),
                _ => q.OrderByDescending(p => p.PaymentDate)
            };
    }
}