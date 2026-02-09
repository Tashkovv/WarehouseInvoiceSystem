namespace WarehouseInvoiceSystem.Domain.InventoryTransaction.Enums
{
    public enum InventoryTransactionType
    {
        Inbound,        // Purchase, return
        Outbound,       // Sale, consumption
        Adjustment,     // Manual correction
        TransferIn,
        TransferOut
    }
}
