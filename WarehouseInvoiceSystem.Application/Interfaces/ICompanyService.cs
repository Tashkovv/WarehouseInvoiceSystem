namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.Company;
    using WarehouseInvoiceSystem.Domain.Enums;

    public interface ICompanyService
    {
        Task<IEnumerable<CompanyDto>> GetAllCompaniesAsync();
        Task<IEnumerable<CompanyDto>> GetCompaniesByTypeAsync(CompanyType type);
        Task<CompanyDto?> GetCompanyByIdAsync(int id);
        Task<CompanyDto> CreateCompanyAsync(CreateCompanyDto createDto);
        Task<CompanyDto> UpdateCompanyAsync(int id, UpdateCompanyDto updateDto);
        Task<bool> DeleteCompanyAsync(int id);
        Task<CompanyBalanceDto> GetCompanyBalanceAsync(int id);
    }
}
