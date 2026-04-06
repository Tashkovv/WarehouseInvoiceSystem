namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.Interfaces;

    /// <summary>
    /// Uses AsyncLocal to flow username across async calls and factory-created DbContexts.
    /// Registered as Singleton — safe because AsyncLocal is per-execution-context.
    /// </summary>
    public class AuditContextService : IAuditContextService
    {
        private static readonly AsyncLocal<string?> _username = new();

        public string? CurrentUsername => _username.Value;

        public void SetUsername(string username) => _username.Value = username;
    }
}
