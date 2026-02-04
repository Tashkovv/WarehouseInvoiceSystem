namespace WarehouseInvoiceSystem.Application.Interfaces
{
    public interface ILocalizationService
    {
        string CurrentLanguage { get; }
        event Action? OnLanguageChanged;
        void SetLanguage(string language);
        string GetString(string key);
    }
}
