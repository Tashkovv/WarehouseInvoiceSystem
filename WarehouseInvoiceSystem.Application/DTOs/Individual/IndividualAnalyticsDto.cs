namespace WarehouseInvoiceSystem.Application.DTOs.Individual
{
    public class IndividualAnalyticsDto
    {
        // Payment Summary
        public int TotalPurchaseNotes { get; set; }
        public decimal TotalAmount { get; set; }
        public int PaidCount { get; set; }
        public decimal PaidAmount { get; set; }
        public int UnpaidCount { get; set; }
        public decimal UnpaidAmount { get; set; }

        // Quick Facts
        public DateTime? FirstPurchaseDate { get; set; }
        public DateTime? LastPurchaseDate { get; set; }
        public string? MostPurchasedProduct { get; set; }
        public decimal MostPurchasedProductQuantity { get; set; }
        public string? MostPurchasedProductUnit { get; set; }

        // Recent Activity
        public List<RecentPurchaseNoteDto> RecentPurchaseNotes { get; set; } = [];
    }

    public class RecentPurchaseNoteDto
    {
        public Guid Id { get; set; }
        public string NoteNumber { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}