namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.DTOs.Company;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Domain.Queries.Results;

    public class CompanyService(ICompanyRepository companyRepository,
                                IInvoiceRepository invoiceRepository) : ICompanyService
    {
        public async Task<IEnumerable<CompanyDto>> GetAllCompaniesAsync(CancellationToken ct = default)
        {
            IEnumerable<Company> companies = await companyRepository.GetAllAsync(ct);
            return companies.Select(MapToDto);
        }

        public async Task<PagedResult<CompanyDto>> GetPagedAsync(GetCompaniesQuery query, CancellationToken ct = default)
        {
            PagedResult<Company> result = await companyRepository.GetPagedAsync(query, ct);
            return new PagedResult<CompanyDto>
            {
                Items = [.. result.Items.Select(MapToDto)],
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<IEnumerable<CompanyDto>> GetActiveCompaniesAsync(CancellationToken ct = default)
        {
            IEnumerable<Company> companies = await companyRepository.GetActiveCompaniesAsync(ct);
            return companies.Select(MapToDto);
        }

        public async Task<IEnumerable<CompanyDto>> GetCompaniesByTypeAsync(CompanyType type, CancellationToken ct = default)
        {
            IEnumerable<Company> companies = await companyRepository.GetByTypeAsync(type, ct);
            return companies.Select(MapToDto);
        }

        public async Task<CompanyDto?> GetCompanyByIdAsync(Guid id, CancellationToken ct = default)
        {
            Company? company = await companyRepository.GetByIdAsync(id, ct);
            return company == null ? null : MapToDto(company);
        }

        public async Task CreateCompanyAsync(CreateCompanyDto createDto)
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

            await companyRepository.CreateAsync(company);
        }

        public async Task UpdateCompanyAsync(Guid id, UpdateCompanyDto updateDto)
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

            await companyRepository.UpdateAsync(company);
        }

        public async Task<bool> DeleteCompanyAsync(Guid id)
        {
            return await companyRepository.DeleteAsync(id);
        }

        public async Task<bool> SetActiveStatusAsync(Guid id, bool isActive)
        {
            Company company = await companyRepository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Company with ID {id} not found");
            company.IsActive = isActive;
            await companyRepository.UpdateAsync(company);
            return true;
        }

        public async Task<CompanyBalanceDto> GetCompanyBalanceAsync(Guid id, CancellationToken ct = default)
        {
            Company? company = await companyRepository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Company with ID {id} not found");

            decimal owedByUs = await companyRepository.GetTotalOwedByCompanyAsync(id, ct);
            decimal owedToUs = await companyRepository.GetTotalOwedToCompanyAsync(id, ct);

            return new CompanyBalanceDto
            {
                CompanyId = company.Id,
                CompanyName = company.Name,
                TotalOwedByUs = owedByUs,
                TotalOwedToUs = owedToUs,
                NetBalance = owedToUs - owedByUs
            };
        }

        public async Task<CompanyAnalyticsDto> GetCompanyAnalyticsAsync(Guid id, CancellationToken ct = default)
        {
            var data = await invoiceRepository.GetCompanyAnalyticsDataAsync(id, ct);

            if (data.StatRows.Count == 0 && data.RecentInvoices.Count == 0)
                return new CompanyAnalyticsDto();

            var analytics = new CompanyAnalyticsDto();
            var rows = data.StatRows;

            // ── Receivables ───────────────────────────────────────────────────
            var recActive = rows.Where(r => r.Type == InvoiceType.Receivable && r.Status != InvoiceStatus.Cancelled && r.Status != InvoiceStatus.Draft).ToList();
            var recPaid   = rows.Where(r => r.Type == InvoiceType.Receivable && r.Status == InvoiceStatus.Paid).ToList();
            var recOpen   = rows.Where(r => r.Type == InvoiceType.Receivable && r.Status is InvoiceStatus.Confirmed
                                                                                             or InvoiceStatus.PartiallyPaid
                                                                                             or InvoiceStatus.Overdue).ToList();
            var recOverdue    = rows.Where(r => r.Type == InvoiceType.Receivable && r.Status == InvoiceStatus.Overdue).ToList();
            var recCancelled  = rows.Where(r => r.Type == InvoiceType.Receivable && r.Status == InvoiceStatus.Cancelled).ToList();

            analytics.ReceivableTotalCount      = recActive.Sum(r => r.Count);
            analytics.ReceivableTotalAmount     = recActive.Sum(r => r.TotalAmount);
            analytics.ReceivablePaidCount       = recPaid.Sum(r => r.Count);
            analytics.ReceivablePaidAmount      = recPaid.Sum(r => r.AmountPaid);
            analytics.ReceivableOpenCount       = recOpen.Sum(r => r.Count);
            analytics.ReceivableAmountDue       = recOpen.Sum(r => r.AmountDue);
            analytics.ReceivableOverdueCount    = recOverdue.Sum(r => r.Count);
            analytics.ReceivableOverdueAmountDue = recOverdue.Sum(r => r.AmountDue);
            analytics.ReceivableCancelledCount  = recCancelled.Sum(r => r.Count);
            analytics.ReceivableCancelledAmount = recCancelled.Sum(r => r.TotalAmount);

            // ── Payables ──────────────────────────────────────────────────────
            var payActive = rows.Where(r => r.Type == InvoiceType.Payable && r.Status != InvoiceStatus.Cancelled && r.Status != InvoiceStatus.Draft).ToList();
            var payPaid   = rows.Where(r => r.Type == InvoiceType.Payable && r.Status == InvoiceStatus.Paid).ToList();
            var payOpen   = rows.Where(r => r.Type == InvoiceType.Payable && r.Status is InvoiceStatus.Confirmed
                                                                                          or InvoiceStatus.PartiallyPaid
                                                                                          or InvoiceStatus.Overdue).ToList();
            var payOverdue   = rows.Where(r => r.Type == InvoiceType.Payable && r.Status == InvoiceStatus.Overdue).ToList();
            var payCancelled = rows.Where(r => r.Type == InvoiceType.Payable && r.Status == InvoiceStatus.Cancelled).ToList();

            analytics.PayableTotalCount      = payActive.Sum(r => r.Count);
            analytics.PayableTotalAmount     = payActive.Sum(r => r.TotalAmount);
            analytics.PayablePaidCount       = payPaid.Sum(r => r.Count);
            analytics.PayablePaidAmount      = payPaid.Sum(r => r.AmountPaid);
            analytics.PayableOpenCount       = payOpen.Sum(r => r.Count);
            analytics.PayableAmountDue       = payOpen.Sum(r => r.AmountDue);
            analytics.PayableOverdueCount    = payOverdue.Sum(r => r.Count);
            analytics.PayableOverdueAmountDue = payOverdue.Sum(r => r.AmountDue);
            analytics.PayableCancelledCount  = payCancelled.Sum(r => r.Count);
            analytics.PayableCancelledAmount = payCancelled.Sum(r => r.TotalAmount);

            // ── Invoice history ───────────────────────────────────────────────
            analytics.FirstInvoiceDate = data.FirstInvoiceDate;
            analytics.LastInvoiceDate  = data.LastInvoiceDate;

            // ── Most traded product ───────────────────────────────────────────
            if (data.MostTradedProductName is not null)
            {
                analytics.MostTradedProduct         = data.MostTradedProductName;
                analytics.MostTradedProductQuantity = data.MostTradedProductQuantity;
                analytics.MostTradedProductUnit     = data.MostTradedProductUnit;
            }

            // ── Recent invoices ───────────────────────────────────────────────
            analytics.RecentInvoices = data.RecentInvoices
                .Select(r => new RecentInvoiceDto
                {
                    Id            = r.Id,
                    InvoiceNumber = r.InvoiceNumber,
                    Type          = r.Type,
                    Status        = r.Status,
                    IssueDate     = r.IssueDate,
                    DueDate       = r.DueDate,
                    TotalAmount   = r.TotalAmount,
                    AmountDue     = r.AmountDue
                })
                .ToList();

            return analytics;
        }

        public Task<PartnerCountsResult> GetPartnerCountsAsync(CancellationToken ct = default)
            => companyRepository.GetPartnerCountsAsync(ct);

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