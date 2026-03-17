namespace WarehouseInvoiceSystem.Domain.Queries.Results
{
    using WarehouseInvoiceSystem.Domain.Enums;

    public class CompanyAnalyticsResult
    {
        public List<CompanyInvoiceStatRow> StatRows { get; set; } = [];
        public string? MostTradedProductName { get; set; }
        public decimal MostTradedProductQuantity { get; set; }
        public string? MostTradedProductUnit { get; set; }
        public DateTime? FirstInvoiceDate { get; set; }
        public DateTime? LastInvoiceDate { get; set; }
        public List<CompanyRecentInvoiceRow> RecentInvoices { get; set; } = [];
    }

    public class CompanyInvoiceStatRow
    {
        public InvoiceType Type { get; set; }
        public InvoiceStatus Status { get; set; }
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal AmountDue { get; set; }
    }

    public class CompanyRecentInvoiceRow
    {
        public Guid Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public InvoiceType Type { get; set; }
        public InvoiceStatus Status { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountDue { get; set; }
    }
}
