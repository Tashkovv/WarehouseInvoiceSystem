namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.Company;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public interface ICompanyService
    {
        Task<IEnumerable<CompanyDto>> GetAllCompaniesAsync();
        Task<PagedResult<CompanyDto>> GetPagedAsync(GetCompaniesQuery query);
        Task<IEnumerable<CompanyDto>> GetActiveCompaniesAsync();
        Task<IEnumerable<CompanyDto>> GetCompaniesByTypeAsync(CompanyType type);
        Task<CompanyDto?> GetCompanyByIdAsync(Guid id);
        Task CreateCompanyAsync(CreateCompanyDto createDto);
        Task UpdateCompanyAsync(Guid id, UpdateCompanyDto updateDto);
        Task<bool> DeleteCompanyAsync(Guid id);
        Task<bool> SetActiveStatusAsync(Guid id, bool isActive);
        Task<CompanyBalanceDto> GetCompanyBalanceAsync(Guid id);
        Task<CompanyAnalyticsDto> GetCompanyAnalyticsAsync(Guid id);
    }
}