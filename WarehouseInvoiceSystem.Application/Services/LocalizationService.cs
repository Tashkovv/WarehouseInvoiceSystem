namespace WarehouseInvoiceSystem.Application.Services
{
    using System.Globalization;
    using System.Reflection;
    using System.Text.Json;
    using WarehouseInvoiceSystem.Application.Interfaces;

    public class LocalizationService : ILocalizationService
    {
        private string _currentLanguage = "mk";

        public string CurrentLanguage => _currentLanguage;
        public string CurrencySymbol => _currentLanguage == "mk" ? "ден." : "MKD";

        public event Action? OnLanguageChanged;

        private readonly Dictionary<string, Dictionary<string, string>> _translations = new()
        {
            ["en"] = LoadLanguage("en"),
            ["mk"] = LoadLanguage("mk"),
        };

        private static Dictionary<string, string> LoadLanguage(string lang)
        {
            string resourceName = $"WarehouseInvoiceSystem.Application.Localization.{lang}.json";
            using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
            return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
        }

        public void SetLanguage(string language)
        {
            if (language != "en" && language != "mk")
                language = "mk";

            CultureInfo culture = new($"{language}-{language.ToUpper()}");

            culture.NumberFormat.CurrencySymbol = language == "mk" ? "ден." : "MKD";
            culture.NumberFormat.CurrencyDecimalDigits = 2;
            culture.NumberFormat.CurrencyPositivePattern = 3;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            _currentLanguage = language;
            OnLanguageChanged?.Invoke();
        }

        public string GetString(string key)
        {
            if (_translations.TryGetValue(_currentLanguage, out Dictionary<string, string>? languageDict) &&
                languageDict.TryGetValue(key, out string? value))
            {
                return value;
            }

            return key;
        }
    }
}
