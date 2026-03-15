namespace WarehouseInvoiceSystem.Domain.Enums
{
    public enum InventoryTransactionType
    {
        Inbound,        // Purchase, return
        Outbound,       // Sale, consumption
        Adjustment,     // Manual correction
        TransferIn,
        TransferOut,
        Reversed        // Cancellation reversal (signed quantity)
    }
}
