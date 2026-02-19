namespace WarehouseInvoiceSystem.Domain.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;

    public interface ICompanyRepository
    {
        Task<IEnumerable<Company>> GetAllAsync();
        Task<IEnumerable<Company>> GetByTypeAsync(CompanyType type);
        Task<Company?> GetByIdAsync(Guid id);
        Task<Company> CreateAsync(Company company);
        Task<Company> UpdateAsync(Company company);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<decimal> GetTotalOwedByCompanyAsync(Guid companyId);
        Task<decimal> GetTotalOwedToCompanyAsync(Guid companyId);
    }
}
