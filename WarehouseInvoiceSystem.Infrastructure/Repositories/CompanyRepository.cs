namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    public class CompanyRepository(ApplicationDbContext context) : ICompanyRepository
    {
        public async Task<IEnumerable<Company>> GetAllAsync()
        {
            return await context.Companies
                .Where(c => c.DeletedOn == null)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<PagedResult<Company>> GetPagedAsync(GetCompaniesQuery query)
        {
            IQueryable<Company> q = context.Companies
                .Where(c => c.DeletedOn == null);

            if (query.Type.HasValue)
                q = q.Where(c => c.Type == query.Type.Value || c.Type == CompanyType.Both);

            if (query.IsActive.HasValue)
                q = q.Where(c => c.IsActive == query.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
                q = q.Where(c => c.Name.Contains(query.Search) ||
                                 (c.ContactPerson != null && c.ContactPerson.Contains(query.Search)) ||
                                 (c.Email != null && c.Email.Contains(query.Search)));

            q = query.SortBy switch
            {
                "Name" => query.SortAscending ? q.OrderBy(c => c.Name) : q.OrderByDescending(c => c.Name),
                "ContactPerson" => query.SortAscending ? q.OrderBy(c => c.ContactPerson) : q.OrderByDescending(c => c.ContactPerson),
                "Email" => query.SortAscending ? q.OrderBy(c => c.Email) : q.OrderByDescending(c => c.Email),
                _ => q.OrderBy(c => c.Name)
            };

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
        }

        public async Task<IEnumerable<Company>> GetActiveCompaniesAsync()
        {
            return await context.Companies
                .Where(c => c.DeletedOn == null && c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Company>> GetByTypeAsync(CompanyType type)
        {
            return await context.Companies
                .Where(c => c.IsActive && (c.Type == type || c.Type == CompanyType.Both) && c.DeletedOn == null)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Company?> GetByIdAsync(Guid id)
        {
            return await context.Companies
                .FirstOrDefaultAsync(c => c.Id == id && c.DeletedOn == null);
        }

        public async Task<Company> CreateAsync(Company company)
        {
            company.CreatedAt = DateTime.UtcNow;
            context.Companies.Add(company);

            await context.SaveChangesAsync();
            return company;
        }

        public async Task<Company> UpdateAsync(Company company)
        {
            context.Companies.Update(company);

            await context.SaveChangesAsync();
            return company;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            Company? company = await context.Companies
                .Where(x => x.Id == id)
                .SingleOrDefaultAsync();
            if (company == null)
            {
                return false;
            }

            // Soft delete
            company.IsActive = false;
            company.DeletedOn = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await context.Companies.AnyAsync(c => c.Id == id && c.IsActive && c.DeletedOn == null);
        }

        public async Task<decimal> GetTotalOwedByCompanyAsync(Guid companyId)
        {
            return await context.Invoices
                .Where(i => i.CompanyId == companyId &&
                            i.Type == InvoiceType.Payable &&
                            i.Status != InvoiceStatus.Paid &&
                            i.Status != InvoiceStatus.Cancelled &&
                            i.DeletedOn == null)
                .SumAsync(i => i.TotalAmount - i.AmountPaid);
        }

        public async Task<decimal> GetTotalOwedToCompanyAsync(Guid companyId)
        {
            return await context.Invoices
                .Where(i => i.CompanyId == companyId &&
                            i.Type == InvoiceType.Receivable &&
                            i.Status != InvoiceStatus.Paid &&
                            i.Status != InvoiceStatus.Cancelled &&
                            i.DeletedOn == null)
                .SumAsync(i => i.TotalAmount - i.AmountPaid);
        }
    }
}
