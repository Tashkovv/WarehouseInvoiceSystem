namespace WarehouseInvoiceSystem.Application.Settings
{
    public class NotificationSettings
    {
        public bool SendEmails { get; set; }
        public int[]? ReceivableDays { get; set; }
        public int[]? PayableDays { get; set; }
    }
}
