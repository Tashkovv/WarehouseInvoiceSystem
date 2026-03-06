namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    public class PaymentRepository(ApplicationDbContext context) : IPaymentRepository
    {
        public async Task<IEnumerable<Payment>> GetAllAsync()
        {
            IEnumerable<Payment> payments = await context.Payments
                .Where(p => p.DeletedOn == null)
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Company)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return payments;
        }

        public async Task<PagedResult<Payment>> GetPagedAsync(GetPaymentsQuery query)
        {
            IQueryable<Payment> q = context.Payments
                .Where(p => p.DeletedOn == null)
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Company);

            if (query.InvoiceId.HasValue)
                q = q.Where(p => p.InvoiceId == query.InvoiceId.Value);

            if (query.PaymentMethod.HasValue)
                q = q.Where(p => p.PaymentMethod == query.PaymentMethod.Value);

            if (query.DateFrom.HasValue)
                q = q.Where(p => p.PaymentDate >= query.DateFrom.Value);

            if (query.DateTo.HasValue)
                q = q.Where(p => p.PaymentDate <= query.DateTo.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
                q = q.Where(p => (p.ReferenceNumber != null && p.ReferenceNumber.Contains(query.Search)) ||
                                 p.Invoice.InvoiceNumber.Contains(query.Search) ||
                                 p.Invoice.Company.Name.Contains(query.Search));

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
        }

        public async Task<IEnumerable<Payment>> GetByInvoiceIdAsync(Guid invoiceId)
        {
            IEnumerable<Payment> payments = await context.Payments
                .Include(p => p.Invoice)
                .Where(p => p.InvoiceId == invoiceId && p.DeletedOn == null)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return payments;
        }

        public async Task<Payment?> GetByIdAsync(Guid id)
        {
            return await context.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Company)
                .FirstOrDefaultAsync(p => p.Id == id && p.DeletedOn == null);
        }

        public async Task<Payment> CreateAsync(Payment payment)
        {
            payment.CreatedAt = DateTime.UtcNow;
            context.Payments.Add(payment);
            await context.SaveChangesAsync();

            // Reload with includes
            Payment? created = await GetByIdAsync(payment.Id);
            return created!;
        }

        public async Task<Payment> UpdateAsync(Payment payment)
        {
            context.Payments.Update(payment);
            await context.SaveChangesAsync();

            // Reload with includes
            Payment? updated = await GetByIdAsync(payment.Id);
            return updated!;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            Payment? payment = await context.Payments.IgnoreQueryFilters()
                                                     .FirstOrDefaultAsync(p => p.Id == id);
            if (payment == null)
                return false;

            payment.DeletedOn = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<decimal> GetTotalPaymentsByInvoiceAsync(Guid invoiceId)
        {
            decimal total = await context.Payments
                .Where(p => p.InvoiceId == invoiceId && p.DeletedOn == null)
                .SumAsync(p => p.Amount);

            return total;
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
