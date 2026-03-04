namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.DTOs.Company;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    public class CompanyService(ICompanyRepository companyRepository) : ICompanyService
    {
        public async Task<IEnumerable<CompanyDto>> GetAllCompaniesAsync()
        {
            IEnumerable<Company> companies = await companyRepository.GetAllAsync();
            return companies.Select(MapToDto);
        }

        public async Task<IEnumerable<CompanyDto>> GetActiveCompaniesAsync()
        {
            IEnumerable<Company> companies = await companyRepository.GetActiveCompaniesAsync();
            return companies.Select(MapToDto);
        }

        public async Task<IEnumerable<CompanyDto>> GetCompaniesByTypeAsync(CompanyType type)
        {
            IEnumerable<Company> companies = await companyRepository.GetByTypeAsync(type);
            return companies.Select(MapToDto);
        }

        public async Task<CompanyDto?> GetCompanyByIdAsync(Guid id)
        {
            Company? company = await companyRepository.GetByIdAsync(id);
            return company == null ? null : MapToDto(company);
        }

        public async Task<CompanyDto> CreateCompanyAsync(CreateCompanyDto createDto)
        {
            Company company = new()
            {
                Name = createDto.Name,
                Type = createDto.Type,
                ContactPerson = createDto.ContactPerson,
                Email = createDto.Email,
                Phone = createDto.Phone,
                Address = createDto.Address,
                TaxId = createDto.TaxId,
                PaymentTermsDays = createDto.PaymentTermsDays,
                CreditLimit = createDto.CreditLimit,
                IsActive = true
            };

            Company created = await companyRepository.CreateAsync(company);
            return MapToDto(created);
        }

        public async Task<CompanyDto> UpdateCompanyAsync(Guid id, UpdateCompanyDto updateDto)
        {
            Company company = await companyRepository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Company with ID {id} not found");

            company.Name = updateDto.Name;
            company.Type = updateDto.Type;
            company.ContactPerson = updateDto.ContactPerson;
            company.Email = updateDto.Email;
            company.Phone = updateDto.Phone;
            company.Address = updateDto.Address;
            company.TaxId = updateDto.TaxId;
            company.PaymentTermsDays = updateDto.PaymentTermsDays;
            company.CreditLimit = updateDto.CreditLimit;
            company.IsActive = updateDto.IsActive;

            Company updated = await companyRepository.UpdateAsync(company);
            return MapToDto(updated);
        }

        public async Task<bool> DeleteCompanyAsync(Guid id)
        {
            return await companyRepository.DeleteAsync(id);
        }

        public async Task<CompanyBalanceDto> GetCompanyBalanceAsync(Guid id)
        {
            Company? company = await companyRepository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Company with ID {id} not found");

            decimal owedByUs = await companyRepository.GetTotalOwedByCompanyAsync(id);
            decimal owedToUs = await companyRepository.GetTotalOwedToCompanyAsync(id);

            return new CompanyBalanceDto
            {
                CompanyId = company.Id,
                CompanyName = company.Name,
                TotalOwedByUs = owedByUs,
                TotalOwedToUs = owedToUs,
                NetBalance = owedToUs - owedByUs
            };
        }

        private static CompanyDto MapToDto(Company company)
        {
            return new CompanyDto
            {
                Id = company.Id,
                Name = company.Name,
                Type = company.Type,
                ContactPerson = company.ContactPerson,
                Email = company.Email,
                Phone = company.Phone,
                Address = company.Address,
                TaxId = company.TaxId,
                PaymentTermsDays = company.PaymentTermsDays,
                CreditLimit = company.CreditLimit,
                IsActive = company.IsActive,
                CreatedAt = company.CreatedAt,
            };
        }
    }
}
