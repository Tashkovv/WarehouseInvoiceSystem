namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.DTOs.Company;
    using WarehouseInvoiceSystem.Application.DTOs.Invoice;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public class CompanyService(ICompanyRepository companyRepository,
                                IInvoiceService invoiceService) : ICompanyService
    {
        public async Task<IEnumerable<CompanyDto>> GetAllCompaniesAsync()
        {
            IEnumerable<Company> companies = await companyRepository.GetAllAsync();
            return companies.Select(MapToDto);
        }

        public async Task<PagedResult<CompanyDto>> GetPagedAsync(GetCompaniesQuery query)
        {
            PagedResult<Company> result = await companyRepository.GetPagedAsync(query);
            return new PagedResult<CompanyDto>
            {
                Items = [.. result.Items.Select(MapToDto)],
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
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

        public async Task<bool> SetActiveStatusAsync(Guid id, bool isActive)
        {
            Company company = await companyRepository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Company with ID {id} not found");
            company.IsActive = isActive;
            await companyRepository.UpdateAsync(company);
            return true;
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

        public async Task<CompanyAnalyticsDto> GetCompanyAnalyticsAsync(Guid id)
        {
            var analytics = new CompanyAnalyticsDto();

            IEnumerable<InvoiceDto> allInvoices = await invoiceService.GetInvoicesByCompanyAsync(id);
            var invoiceList = allInvoices.ToList();

            if (invoiceList.Count == 0)
                return analytics;

            // ── Split by type ─────────────────────────────────────────────────
            var receivables = invoiceList.Where(i => i.Type == InvoiceType.Receivable).ToList();
            var payables = invoiceList.Where(i => i.Type == InvoiceType.Payable).ToList();

            // ── Receivables ───────────────────────────────────────────────────
            var receivableCancelled = receivables.Where(i => i.Status == InvoiceStatus.Cancelled).ToList();
            var receivableActive = receivables.Where(i => i.Status != InvoiceStatus.Cancelled).ToList();
            var receivablePaid = receivables.Where(i => i.Status == InvoiceStatus.Paid).ToList();
            var receivableOpen = receivables.Where(i => i.Status is InvoiceStatus.Draft
                                                                       or InvoiceStatus.Sent
                                                                       or InvoiceStatus.PartiallyPaid
                                                                       or InvoiceStatus.Overdue).ToList();
            var receivableOverdue = receivables.Where(i => i.Status == InvoiceStatus.Overdue).ToList();

            analytics.ReceivableTotalCount = receivableActive.Count;
            analytics.ReceivableTotalAmount = receivableActive.Sum(i => i.TotalAmount);
            analytics.ReceivablePaidCount = receivablePaid.Count;
            analytics.ReceivablePaidAmount = receivablePaid.Sum(i => i.TotalAmount);
            analytics.ReceivableOpenCount = receivableOpen.Count;
            analytics.ReceivableAmountDue = receivableOpen.Sum(i => i.AmountDue);
            analytics.ReceivableOverdueCount = receivableOverdue.Count;
            analytics.ReceivableOverdueAmountDue = receivableOverdue.Sum(i => i.AmountDue);
            analytics.ReceivableCancelledCount = receivableCancelled.Count;
            analytics.ReceivableCancelledAmount = receivableCancelled.Sum(i => i.TotalAmount);

            // ── Payables ──────────────────────────────────────────────────────
            var payableCancelled = payables.Where(i => i.Status == InvoiceStatus.Cancelled).ToList();
            var payableActive = payables.Where(i => i.Status != InvoiceStatus.Cancelled).ToList();
            var payablePaid = payables.Where(i => i.Status == InvoiceStatus.Paid).ToList();
            var payableOpen = payables.Where(i => i.Status is InvoiceStatus.Draft
                                                                  or InvoiceStatus.Sent
                                                                  or InvoiceStatus.PartiallyPaid
                                                                  or InvoiceStatus.Overdue).ToList();
            var payableOverdue = payables.Where(i => i.Status == InvoiceStatus.Overdue).ToList();

            analytics.PayableTotalCount = payableActive.Count;
            analytics.PayableTotalAmount = payableActive.Sum(i => i.TotalAmount);
            analytics.PayablePaidCount = payablePaid.Count;
            analytics.PayablePaidAmount = payablePaid.Sum(i => i.TotalAmount);
            analytics.PayableOpenCount = payableOpen.Count;
            analytics.PayableAmountDue = payableOpen.Sum(i => i.AmountDue);
            analytics.PayableOverdueCount = payableOverdue.Count;
            analytics.PayableOverdueAmountDue = payableOverdue.Sum(i => i.AmountDue);
            analytics.PayableCancelledCount = payableCancelled.Count;
            analytics.PayableCancelledAmount = payableCancelled.Sum(i => i.TotalAmount);

            // ── Invoice history ───────────────────────────────────────────────
            var activeInvoices = invoiceList.Where(i => i.Status != InvoiceStatus.Cancelled).ToList();
            if (activeInvoices.Count > 0)
            {
                analytics.FirstInvoiceDate = activeInvoices.Min(i => i.IssueDate);
                analytics.LastInvoiceDate = activeInvoices.Max(i => i.IssueDate);
            }

            // ── Most traded product (active invoices, both types) ─────────────
            var productStats = activeInvoices
                .SelectMany(i => i.LineItems)
                .GroupBy(li => new { li.ProductId, li.ProductName, li.ProductUnit })
                .Select(g => new
                {
                    g.Key.ProductName,
                    g.Key.ProductUnit,
                    TotalQuantity = g.Sum(li => li.Quantity)
                })
                .OrderByDescending(p => p.TotalQuantity)
                .FirstOrDefault();

            if (productStats != null)
            {
                analytics.MostTradedProduct = productStats.ProductName;
                analytics.MostTradedProductQuantity = productStats.TotalQuantity;
                analytics.MostTradedProductUnit = productStats.ProductUnit;
            }

            // ── Recent invoices ───────────────────────────────────────────────
            analytics.RecentInvoices = invoiceList
                .OrderByDescending(i => i.IssueDate)
                .Take(5)
                .Select(i => new RecentInvoiceDto
                {
                    Id = i.Id,
                    InvoiceNumber = i.InvoiceNumber,
                    Type = i.Type,
                    Status = i.Status,
                    IssueDate = i.IssueDate,
                    DueDate = i.DueDate,
                    TotalAmount = i.TotalAmount,
                    AmountDue = i.AmountDue
                })
                .ToList();

            return analytics;
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