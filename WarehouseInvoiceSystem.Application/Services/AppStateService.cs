namespace WarehouseInvoiceSystem.Infrastructure.Common
{
    using System.Text.Json;
    using Microsoft.Extensions.Hosting;
    using WarehouseInvoiceSystem.Application.Interfaces;

    /// <summary>
    /// Stores app state as a JSON file in the application's content root.
    /// Thread-safe via SemaphoreSlim — the BackgroundJobWorker is the only writer,
    /// but multiple concurrent reads are safe.
    /// </summary>
    public class AppStateService(IHostEnvironment environment) : IAppStateService
    {
        private readonly string _filePath = Path.Combine(
            environment.ContentRootPath, "app-state.json");

        private readonly SemaphoreSlim _lock = new(1, 1);

        public async Task<DateTime?> GetDateAsync(string key, CancellationToken ct = default)
        {
            Dictionary<string, string> state = await ReadAsync();
            return state.TryGetValue(key, out string? value) && DateTime.TryParse(value, out DateTime result)
                ? result
                : null;
        }

        public async Task SetDateAsync(string key, DateTime value)
        {
            await _lock.WaitAsync();
            try
            {
                Dictionary<string, string> state = await ReadAsync();
                state[key] = value.ToString("O"); // Round-trip ISO 8601
                string json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_filePath, json);
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<Dictionary<string, string>> ReadAsync()
        {
            if (!File.Exists(_filePath))
                return new Dictionary<string, string>();

            try
            {
                string json = await File.ReadAllTextAsync(_filePath);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                       ?? new Dictionary<string, string>();
            }
            catch (JsonException)
            {
                // Corrupted file — start fresh
                return new Dictionary<string, string>();
            }
        }
    }
}