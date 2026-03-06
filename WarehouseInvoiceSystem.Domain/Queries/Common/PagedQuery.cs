namespace WarehouseInvoiceSystem.Domain.Queries.Common
{
    public abstract class PagedQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; }
        public bool SortAscending { get; set; } = false;
        public string? Search { get; set; }
    }
}