namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.Company;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public interface ICompanyService
    {
        Task<IEnumerable<CompanyDto>> GetAllCompaniesAsync(CancellationToken ct = default);
        Task<PagedResult<CompanyDto>> GetPagedAsync(GetCompaniesQuery query, CancellationToken ct = default);
        Task<IEnumerable<CompanyDto>> GetActiveCompaniesAsync(CancellationToken ct = default);
        Task<IEnumerable<CompanyDto>> GetCompaniesByTypeAsync(CompanyType type, CancellationToken ct = default);
        Task<CompanyDto?> GetCompanyByIdAsync(Guid id, CancellationToken ct = default);
        Task CreateCompanyAsync(CreateCompanyDto createDto);
        Task UpdateCompanyAsync(Guid id, UpdateCompanyDto updateDto);
        Task<bool> DeleteCompanyAsync(Guid id);
        Task<bool> SetActiveStatusAsync(Guid id, bool isActive);
        Task<CompanyBalanceDto> GetCompanyBalanceAsync(Guid id, CancellationToken ct = default);
        Task<CompanyAnalyticsDto> GetCompanyAnalyticsAsync(Guid id, CancellationToken ct = default);
    }
}