using WarehouseInvoiceSystem.Domain.Enums;

namespace WarehouseInvoiceSystem.Application.DTOs.Company
{
    public class CompanyAnalyticsDto
    {
        // ── Receivables (we issued to them, they owe us) ──────────────────────
        public int ReceivableTotalCount { get; set; }
        public decimal ReceivableTotalAmount { get; set; }

        public int ReceivablePaidCount { get; set; }
        public decimal ReceivablePaidAmount { get; set; }

        public int ReceivableOpenCount { get; set; }       // Draft + Sent + PartiallyPaid + Overdue
        public decimal ReceivableAmountDue { get; set; }   // Outstanding balance on open receivables

        public int ReceivableOverdueCount { get; set; }
        public decimal ReceivableOverdueAmountDue { get; set; }

        public int ReceivableCancelledCount { get; set; }
        public decimal ReceivableCancelledAmount { get; set; }

        // ── Payables (they issued to us, we owe them) ─────────────────────────
        public int PayableTotalCount { get; set; }
        public decimal PayableTotalAmount { get; set; }

        public int PayablePaidCount { get; set; }
        public decimal PayablePaidAmount { get; set; }

        public int PayableOpenCount { get; set; }          // Draft + Sent + PartiallyPaid + Overdue
        public decimal PayableAmountDue { get; set; }      // Outstanding balance on open payables

        public int PayableOverdueCount { get; set; }
        public decimal PayableOverdueAmountDue { get; set; }

        public int PayableCancelledCount { get; set; }
        public decimal PayableCancelledAmount { get; set; }

        // ── Invoice history ───────────────────────────────────────────────────
        public DateTime? FirstInvoiceDate { get; set; }
        public DateTime? LastInvoiceDate { get; set; }

        // ── Most traded product (across all active invoices, both types) ──────
        public string? MostTradedProduct { get; set; }
        public decimal MostTradedProductQuantity { get; set; }
        public string? MostTradedProductUnit { get; set; }

        // ── Recent invoices ───────────────────────────────────────────────────
        public List<RecentInvoiceDto> RecentInvoices { get; set; } = [];
    }

    public class RecentInvoiceDto
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