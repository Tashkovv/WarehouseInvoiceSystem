namespace WarehouseInvoiceSystem.Domain.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Company.Domain;
    using WarehouseInvoiceSystem.Domain.Company.Enums;

    public interface ICompanyRepository
    {
        Task<IEnumerable<Company>> GetAllAsync();
        Task<IEnumerable<Company>> GetByTypeAsync(CompanyType type);
        Task<Company?> GetByIdAsync(int id);
        Task<Company> CreateAsync(Company company);
        Task<Company> UpdateAsync(Company company);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<decimal> GetTotalOwedByCompanyAsync(int companyId);
        Task<decimal> GetTotalOwedToCompanyAsync(int companyId);
    }
}
