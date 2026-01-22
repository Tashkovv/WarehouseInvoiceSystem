namespace WarehouseInvoiceSystem.Domain.Enums
{
    public enum InvoiceType
    {
        Receivable = 1,  // Money owed TO the warehouse (client owes us)
        Payable = 2      // Money owed BY the warehouse (we owe vendor)
    }
}
