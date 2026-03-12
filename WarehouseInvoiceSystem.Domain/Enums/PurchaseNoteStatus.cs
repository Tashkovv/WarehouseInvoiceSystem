namespace WarehouseInvoiceSystem.Domain.Enums
{
    public enum PurchaseNoteStatus
    {
        Draft = 1,  // Saved, goods not yet received, no stock movement
        Pending = 2,  // Goods received, inventory created, payment outstanding
        Paid = 3,  // Payment settled — terminal, fully locked
        Cancelled = 4   // Cancelled — terminal, stock reversed if was Pending
    }
}
