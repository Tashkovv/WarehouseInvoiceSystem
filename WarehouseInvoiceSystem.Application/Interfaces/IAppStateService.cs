namespace WarehouseInvoiceSystem.Application.Interfaces
{
    /// <summary>
    /// Persists lightweight key/value state that must survive application restarts.
    /// Backed by a JSON file in the application's content root — no migration required.
    /// </summary>
    public interface IAppStateService
    {
        Task<DateTime?> GetDateAsync(string key, CancellationToken ct = default);
        Task SetDateAsync(string key, DateTime value);
    }
}