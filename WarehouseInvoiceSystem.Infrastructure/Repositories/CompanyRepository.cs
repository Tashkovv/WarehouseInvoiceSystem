namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    public class CompanyRepository(IDbContextFactory<ApplicationDbContext> factory)
        : BaseRepository(factory), ICompanyRepository
    {
        public Task<IEnumerable<Company>> GetAllAsync() =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<Company>)await All<Company>(context)
                    .OrderBy(c => c.Name)
                    .ToListAsync();
            });

        public Task<PagedResult<Company>> GetPagedAsync(GetCompaniesQuery query) =>
            WithContextAsync(async context =>
            {
                IQueryable<Company> q = ApplyFilters(All<Company>(context), query);
                q = ApplySort(q, query.SortBy, query.SortAscending);

                int totalCount = await q.CountAsync();

                List<Company> items = await q
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync();

                return new PagedResult<Company>
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = query.Page,
                    PageSize = query.PageSize
                };
            });

        public Task<IEnumerable<Company>> GetActiveCompaniesAsync() =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<Company>)await All<Company>(context)
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync();
            });

        public Task<IEnumerable<Company>> GetByTypeAsync(CompanyType type) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<Company>)await All<Company>(context)
                    .Where(c => c.IsActive && (c.Type == type || c.Type == CompanyType.Both))
                    .OrderBy(c => c.Name)
                    .ToListAsync();
            });

        public Task<Company?> GetByIdAsync(Guid id) =>
            WithContextAsync(context =>
                All<Company>(context).FirstOrDefaultAsync(c => c.Id == id));

        public Task CreateAsync(Company company) =>
            WithContextAsync(async context =>
            {
                company.CreatedAt = DateTime.UtcNow;
                context.Companies.Add(company);
                await SaveAsync(context);
            });

        public Task UpdateAsync(Company company) =>
            WithContextAsync(async context =>
            {
                context.Companies.Update(company);
                await SaveAsync(context);
            });

        public Task<bool> DeleteAsync(Guid id) =>
            WithContextAsync(async context =>
            {
                Company? company = await context.Companies
                    .Where(x => x.Id == id)
                    .SingleOrDefaultAsync();

                if (company == null)
                    return false;

                company.IsActive = false;
                company.DeletedOn = DateTime.UtcNow;
                await SaveAsync(context);
                return true;
            });

        public Task<bool> ExistsAsync(Guid id) =>
            WithContextAsync(context =>
                All<Company>(context).AnyAsync(c => c.Id == id && c.IsActive));

        public Task<decimal> GetTotalOwedByCompanyAsync(Guid companyId) =>
            WithContextAsync(context =>
                All<Invoice>(context)
                    .Where(i => i.CompanyId == companyId &&
                                i.Type == InvoiceType.Payable &&
                                i.Status != InvoiceStatus.Paid &&
                                i.Status != InvoiceStatus.Cancelled)
                    .SumAsync(i => i.TotalAmount - i.AmountPaid));

        public Task<decimal> GetTotalOwedToCompanyAsync(Guid companyId) =>
            WithContextAsync(context =>
                All<Invoice>(context)
                    .Where(i => i.CompanyId == companyId &&
                                i.Type == InvoiceType.Receivable &&
                                i.Status != InvoiceStatus.Paid &&
                                i.Status != InvoiceStatus.Cancelled)
                    .SumAsync(i => i.TotalAmount - i.AmountPaid));

        private static IQueryable<Company> ApplyFilters(IQueryable<Company> q, GetCompaniesQuery query)
        {
            if (query.Type.HasValue)
                q = q.Where(c => c.Type == query.Type.Value || c.Type == CompanyType.Both);

            if (query.IsActive.HasValue)
                q = q.Where(c => c.IsActive == query.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                string search = $"%{query.Search}%";
                q = q.Where(c =>
                    EF.Functions.ILike(c.Name, search) ||
                    (c.ContactPerson != null && EF.Functions.ILike(c.ContactPerson, search)) ||
                    (c.Email != null && EF.Functions.ILike(c.Email, search)));
            }

            return q;
        }

        private static IQueryable<Company> ApplySort(IQueryable<Company> q, string? sortBy, bool ascending)
            => sortBy switch
            {
                "Name" => ascending ? q.OrderBy(c => c.Name) : q.OrderByDescending(c => c.Name),
                "ContactPerson" => ascending ? q.OrderBy(c => c.ContactPerson) : q.OrderByDescending(c => c.ContactPerson),
                "Email" => ascending ? q.OrderBy(c => c.Email) : q.OrderByDescending(c => c.Email),
                _ => q.OrderByDescending(c => c.CreatedAt)
            };
    }
}