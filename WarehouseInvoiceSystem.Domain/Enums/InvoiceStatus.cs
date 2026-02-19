namespace WarehouseInvoiceSystem.Domain.Enums
{
    public enum InvoiceStatus
    {
        Draft = 1,         // Being created, not sent yet
        Sent = 2,          // Sent to client/vendor
        PartiallyPaid = 3, // Some payment received
        Paid = 4,          // Fully paid
        Overdue = 5,       // Past due date and unpaid
        Cancelled = 6      // Cancelled/void
    }
}
