namespace WarehouseInvoiceSystem.Domain.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Domain.Queries.Results;

    public interface ICompanyRepository
    {
        Task<IEnumerable<Company>> GetAllAsync(CancellationToken ct = default);
        Task<PagedResult<Company>> GetPagedAsync(GetCompaniesQuery query, CancellationToken ct = default);

        Task<IEnumerable<Company>> GetActiveCompaniesAsync(CancellationToken ct = default);
        Task<IEnumerable<Company>> GetByTypeAsync(CompanyType type, CancellationToken ct = default);

        Task<Company?> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task CreateAsync(Company company);
        Task UpdateAsync(Company company);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

        Task<decimal> GetTotalOwedByCompanyAsync(Guid companyId, CancellationToken ct = default);
        Task<decimal> GetTotalOwedToCompanyAsync(Guid companyId, CancellationToken ct = default);
        Task<PartnerCountsResult> GetPartnerCountsAsync(CancellationToken ct = default);
    }
}