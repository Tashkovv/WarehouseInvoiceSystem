namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Domain.Queries.Results;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    public class CompanyRepository(IDbContextFactory<ApplicationDbContext> factory, IAuditContextService auditContext)
        : BaseRepository(factory, auditContext), ICompanyRepository
    {
        public Task<IEnumerable<Company>> GetAllAsync(CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<Company>)await All<Company>(context)
                    .OrderBy(c => c.Name)
                    .ToListAsync(ct);
            });

        public Task<PagedResult<Company>> GetPagedAsync(GetCompaniesQuery query, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                IQueryable<Company> q = ApplyFilters(All<Company>(context), query);
                q = ApplySort(q, query.SortBy, query.SortAscending);

                int totalCount = await q.CountAsync(ct);

                List<Company> items = await q
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync(ct);

                return new PagedResult<Company>
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = query.Page,
                    PageSize = query.PageSize
                };
            });

        public Task<IEnumerable<Company>> GetActiveCompaniesAsync(CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<Company>)await All<Company>(context)
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync(ct);
            });

        public Task<IEnumerable<Company>> GetByTypeAsync(CompanyType type, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<Company>)await All<Company>(context)
                    .Where(c => c.IsActive && (c.Type == type || c.Type == CompanyType.Both))
                    .OrderBy(c => c.Name)
                    .ToListAsync(ct);
            });

        public Task<Company?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            WithContextAsync(context =>
                All<Company>(context).FirstOrDefaultAsync(c => c.Id == id, ct));

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
                Company? tracked = await context.Companies.FindAsync(company.Id)
                    ?? throw new KeyNotFoundException($"Company {company.Id} not found");
                context.Entry(tracked).CurrentValues.SetValues(company);
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

        public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) =>
            WithContextAsync(context =>
                All<Company>(context).AnyAsync(c => c.Id == id && c.IsActive, ct));

        public Task<decimal> GetTotalOwedByCompanyAsync(Guid companyId, CancellationToken ct = default) =>
            WithContextAsync(context =>
                All<Invoice>(context)
                    .Where(i => i.CompanyId == companyId &&
                                i.Type == InvoiceType.Payable &&
                                i.Status != InvoiceStatus.Paid &&
                                i.Status != InvoiceStatus.Cancelled)
                    .SumAsync(i => i.TotalAmount - i.AmountPaid));

        public Task<decimal> GetTotalOwedToCompanyAsync(Guid companyId, CancellationToken ct = default) =>
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
                q = q.Where(c => c.IsActive == query.IsActive.Value || c.IsActive);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                string search = $"%{query.Search}%";
                q = query.SearchByNameOnly
                    ? q.Where(c => EF.Functions.ILike(c.Name, search))
                    : q.Where(c =>
                        EF.Functions.ILike(c.Name, search) ||
                        (c.ContactPerson != null && EF.Functions.ILike(c.ContactPerson, search)) ||
                        (c.Email != null && EF.Functions.ILike(c.Email, search)));
            }

            return q;
        }

        public Task<PartnerCountsResult> GetPartnerCountsAsync(CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                IQueryable<Company> q = All<Company>(context);
                return new PartnerCountsResult
                {
                    Total   = await q.CountAsync(ct),
                    Active  = await q.CountAsync(c => c.IsActive, ct),
                    Clients = await q.CountAsync(c => c.Type == CompanyType.Client || c.Type == CompanyType.Both, ct),
                    Vendors = await q.CountAsync(c => c.Type == CompanyType.Vendor || c.Type == CompanyType.Both, ct)
                };
            });

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