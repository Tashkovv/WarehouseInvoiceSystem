namespace WarehouseInvoiceSystem.Domain.Queries.Results
{
    using WarehouseInvoiceSystem.Domain.Enums;

    public class IndividualAnalyticsResult
    {
        public List<IndividualNoteStatRow> StatRows { get; set; } = [];
        public string? MostPurchasedProductName { get; set; }
        public decimal MostPurchasedProductQuantity { get; set; }
        public string? MostPurchasedProductUnit { get; set; }
        public DateTime? FirstPurchaseDate { get; set; }
        public DateTime? LastPurchaseDate { get; set; }
        public List<IndividualRecentNoteRow> RecentNotes { get; set; } = [];
    }

    public class IndividualNoteStatRow
    {
        public PurchaseNoteStatus Status { get; set; }
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class IndividualRecentNoteRow
    {
        public Guid Id { get; set; }
        public string NoteNumber { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public decimal TotalAmount { get; set; }
        public PurchaseNoteStatus Status { get; set; }
    }
}
