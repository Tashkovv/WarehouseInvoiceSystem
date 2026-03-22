namespace WarehouseInvoiceSystem.Domain.Entities
{
    public class NotificationInvoice
    {
        public Guid Id { get; set; }
        public Guid NotificationId { get; set; }
        public Guid InvoiceId { get; set; }

        // Navigation
        public Notification Notification { get; set; } = null!;
        public Invoice Invoice { get; set; } = null!;
    }
}
