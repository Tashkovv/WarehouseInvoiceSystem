namespace WarehouseInvoiceSystem.Application.Queries
{
    using WarehouseInvoiceSystem.Application.DTOs.Common;
    using WarehouseInvoiceSystem.Domain.Enums;

    public class GetCompaniesQuery : PagedQuery
    {
        public CompanyType? Type { get; set; }
        public bool? IsActive { get; set; }
    }
}