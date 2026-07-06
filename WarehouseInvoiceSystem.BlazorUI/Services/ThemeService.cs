namespace WarehouseInvoiceSystem.BlazorUI.Services
{
    using Microsoft.JSInterop;

    public class ThemeService(IJSRuntime jsRuntime) : IThemeService
    {
        private bool _isDarkMode;
        private bool _initialized;

        public bool IsDarkMode => _isDarkMode;

        public event Action? OnThemeChanged;

        public async Task InitializeAsync()
        {
            if (_initialized)
                return;

            _initialized = true;

            try
            {
                string? stored = await jsRuntime.InvokeAsync<string?>("wisTheme.getPreference");
                bool resolved = stored == "dark";

                if (resolved != _isDarkMode)
                {
                    _isDarkMode = resolved;
                    OnThemeChanged?.Invoke();
                }
            }
            catch (JSDisconnectedException)
            {
                // Circuit torn down mid-call; nothing to do.
            }
        }

        public async Task SetDarkModeAsync(bool isDark)
        {
            if (_isDarkMode == isDark)
                return;

            _isDarkMode = isDark;
            OnThemeChanged?.Invoke();

            await jsRuntime.InvokeVoidAsync("wisTheme.setPreference", isDark ? "dark" : "light");
        }
    }
}
