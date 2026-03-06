namespace WarehouseInvoiceSystem.Domain.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public interface ICompanyRepository
    {
        Task<IEnumerable<Company>> GetAllAsync();
        Task<PagedResult<Company>> GetPagedAsync(GetCompaniesQuery query);
        Task<IEnumerable<Company>> GetActiveCompaniesAsync();
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
