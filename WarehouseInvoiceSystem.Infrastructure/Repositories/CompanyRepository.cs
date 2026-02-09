namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Domain.Company.Domain;
    using WarehouseInvoiceSystem.Domain.Company.Enums;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Invoice.Enums;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    public class CompanyRepository(ApplicationDbContext context) : ICompanyRepository
    {
        public async Task<IEnumerable<Company>> GetAllAsync()
        {
            return await context.Companies
                .Where(c => c.IsActive && c.DeletedOn == null)
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

        public async Task<Company?> GetByIdAsync(int id)
        {
            return await context.Companies
                .Include(c => c.Invoices)
                .FirstOrDefaultAsync(c => c.Id == id && c.DeletedOn == null);
        }

        public async Task<Company> CreateAsync(Company company)
        {
            company.CreatedAt = DateTime.Now;
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

        public async Task<bool> DeleteAsync(int id)
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
            company.DeletedOn = DateTime.Now;

            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await context.Companies.AnyAsync(c => c.Id == id && c.IsActive && c.DeletedOn == null);
        }

        public async Task<decimal> GetTotalOwedByCompanyAsync(int companyId)
        {
            return await context.Invoices
                .Where(i => i.CompanyId == companyId &&
                            i.Type == InvoiceType.Payable &&
                            i.Status != InvoiceStatus.Paid &&
                            i.Status != InvoiceStatus.Cancelled &&
                            i.DeletedOn == null)
                .SumAsync(i => i.TotalAmount - i.AmountPaid);
        }

        public async Task<decimal> GetTotalOwedToCompanyAsync(int companyId)
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
