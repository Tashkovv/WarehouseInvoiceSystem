namespace WarehouseInvoiceSystem.Domain.Entities
{
    /// <summary>
    /// Runtime POCO deserialized from the license token payload.
    /// Not a DB entity — stored in app-state.json.
    /// </summary>
    public class LicenseInfo
    {
        public string TenantId { get; init; } = "";
        public string HardwareId { get; init; } = "";
        public DateTime ExpiryDate { get; init; }
        public DateTime LastSeenUtc { get; set; }
        public bool ClockTamperingDetected { get; set; }
    }
}
