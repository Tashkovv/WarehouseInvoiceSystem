namespace WarehouseInvoiceSystem.BlazorUI.Services
{
    public interface IThemeService
    {
        bool IsDarkMode { get; }

        event Action? OnThemeChanged;

        Task InitializeAsync();

        Task SetDarkModeAsync(bool isDark);
    }
}
