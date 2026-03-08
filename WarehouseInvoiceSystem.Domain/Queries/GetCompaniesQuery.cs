namespace WarehouseInvoiceSystem.Domain.Queries
{
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public class GetCompaniesQuery : PagedQuery
    {
        public CompanyType? Type { get; set; }
        public bool? IsActive { get; set; }
    }
}