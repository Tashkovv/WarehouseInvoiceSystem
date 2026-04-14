namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.Interfaces;

    /// <summary>
    /// Scoped service that holds the current username for the Blazor circuit lifetime.
    /// Set once in PageBase.OnInitializedAsync, read by BaseRepository when stamping contexts.
    /// </summary>
    public class AuditContextService : IAuditContextService
    {
        public string? CurrentUsername { get; private set; }

        public void SetUsername(string username) => CurrentUsername = username;
    }
}
