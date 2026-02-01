namespace WarehouseInvoiceSystem.BlazorUI.Services
{
    public class LocalizationService : ILocalizationService
    {
        private string _currentLanguage = "mk";

        public string CurrentLanguage => _currentLanguage;

        public event Action? OnLanguageChanged;

        private readonly Dictionary<string, Dictionary<string, string>> _translations = new()
        {
            ["en"] = new Dictionary<string, string>
            {
                // Navigation
                ["Dashboard"] = "Dashboard",
                ["Companies"] = "Companies",
                ["Invoices"] = "Invoices",
                ["Payments"] = "Payments",
                ["Settings"] = "Settings",
                ["Notifications"] = "Notifications",

                // Dashboard
                ["TotalDue"] = "Total Due",
                ["TotalInvoices"] = "Total Invoices",
                ["Overdue"] = "Overdue",
                ["TotalPaid"] = "Total Paid",
                ["InvoicesUnpaid"] = "invoices unpaid",
                ["Paid"] = "paid",
                ["RequiresAttention"] = "Requires attention",
                ["AllTimeCollected"] = "All time collected",
                ["RecentInvoices"] = "Recent Invoices",
                ["NewInvoice"] = "New Invoice",
                ["NoInvoicesFound"] = "No invoices found",
                ["InvoiceNumber"] = "Invoice #",
                ["Company"] = "Company",
                ["Amount"] = "Amount",
                ["Status"] = "Status",
                ["DueDate"] = "Due Date",
                ["Actions"] = "Actions",
                ["ViewAllInvoices"] = "View All Invoices",

                // Status
                ["Draft"] = "Draft",
                ["Sent"] = "Sent",
                ["PartiallyPaid"] = "Partially Paid",
                ["PaidStatus"] = "Paid",
                ["OverdueStatus"] = "Overdue",
                ["Cancelled"] = "Cancelled",

                // Common
                ["Loading"] = "Loading...",
                ["Save"] = "Save",
                ["Cancel"] = "Cancel",
                ["Delete"] = "Delete",
                ["Edit"] = "Edit",
                ["View"] = "View",
                ["Create"] = "Create",
                ["Update"] = "Update",
                ["Search"] = "Search",
                ["Filter"] = "Filter",
                ["Export"] = "Export",
                ["Import"] = "Import",
                ["WarehouseInvoiceSystem"] = "Warehouse Invoice System"
            },
            ["mk"] = new Dictionary<string, string>
            {
                // Navigation
                ["Dashboard"] = "Контролна табла",
                ["Companies"] = "Компании",
                ["Invoices"] = "Фактури",
                ["Payments"] = "Плаќања",
                ["Settings"] = "Подесувања",
                ["Notifications"] = "Известувања",

                // Dashboard
                ["TotalDue"] = "Вкупно должење",
                ["TotalInvoices"] = "Вкупно фактури",
                ["Overdue"] = "Задоцнети",
                ["TotalPaid"] = "Вкупно платено",
                ["InvoicesUnpaid"] = "Неплатени фактури",
                ["Paid"] = "Платени",
                ["RequiresAttention"] = "Потребно внимание",
                ["AllTimeCollected"] = "Вкупно наплатено",
                ["RecentInvoices"] = "Последни фактури",
                ["NewInvoice"] = "Нова фактура",
                ["NoInvoicesFound"] = "Нема пронајдени фактури",
                ["InvoiceNumber"] = "Фактура #",
                ["Company"] = "Компанија",
                ["Amount"] = "Износ",
                ["Status"] = "Статус",
                ["DueDate"] = "Рок на плаќање",
                ["Actions"] = "Акции",
                ["ViewAllInvoices"] = "Погледни ги сите фактури",

                // Status
                ["Draft"] = "Нацрт",
                ["Sent"] = "Испратена",
                ["PartiallyPaid"] = "Делумно платена",
                ["PaidStatus"] = "Платена",
                ["OverdueStatus"] = "Задоцнета",
                ["Cancelled"] = "Откажана",

                // Common
                ["Loading"] = "Вчитување...",
                ["Save"] = "Зачувај",
                ["Cancel"] = "Откажи",
                ["Delete"] = "Избриши",
                ["Edit"] = "Измени",
                ["View"] = "Прегледај",
                ["Create"] = "Креирај",
                ["Update"] = "Ажурирај",
                ["Search"] = "Пребарај",
                ["Filter"] = "Филтрирај",
                ["Export"] = "Извези",
                ["Import"] = "Увези",
                ["WarehouseInvoiceSystem"] = "Систем за фактури"
            }
        };

        public void SetLanguage(string language)
        {
            if (language != "en" && language != "mk")
                language = "mk";

            _currentLanguage = language;
            OnLanguageChanged?.Invoke();
        }

        public string GetString(string key)
        {
            if (_translations.TryGetValue(_currentLanguage, out Dictionary<string, string>? languageDict) && languageDict.TryGetValue(key, out string? value))
            {
                return value;
            }

            return key; // Return key if translation not found
        }
    }
}
