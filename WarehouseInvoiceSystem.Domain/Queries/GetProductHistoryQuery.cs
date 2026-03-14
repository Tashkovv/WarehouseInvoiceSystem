namespace WarehouseInvoiceSystem.Domain.Queries
{
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public class GetProductHistoryQuery : PagedQuery
    {
        public Guid ProductId { get; set; }
        public Guid? WarehouseId { get; set; }

        /// <summary>
        /// True = purchased (purchase notes + payable invoices).
        /// False = sold (receivable invoices).
        /// </summary>
        public bool Purchased { get; set; }

        /// <summary>Filter by party name — vendor name for purchased, client name for sold.</summary>
        public string? PartyName { get; set; }

        /// <summary>Filter by company ID — used for invoice lines (both purchased payable and sold receivable).</summary>
        public Guid? CompanyId { get; set; }

        /// <summary>Filter by individual ID — used for purchase note lines (purchased only).</summary>
        public Guid? IndividualId { get; set; }

        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}