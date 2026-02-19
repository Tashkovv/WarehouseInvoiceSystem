namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.Company;
    using WarehouseInvoiceSystem.Domain.Enums;

    public interface ICompanyService
    {
        Task<IEnumerable<CompanyDto>> GetAllCompaniesAsync();
        Task<IEnumerable<CompanyDto>> GetCompaniesByTypeAsync(CompanyType type);
        Task<CompanyDto?> GetCompanyByIdAsync(Guid id);
        Task<CompanyDto> CreateCompanyAsync(CreateCompanyDto createDto);
        Task<CompanyDto> UpdateCompanyAsync(Guid id, UpdateCompanyDto updateDto);
        Task<bool> DeleteCompanyAsync(Guid id);
        Task<CompanyBalanceDto> GetCompanyBalanceAsync(Guid id);
    }
}
