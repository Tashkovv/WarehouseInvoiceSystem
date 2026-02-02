namespace WarehouseInvoiceSystem.BlazorUI.Services
{
    public class LocalizationService : ILocalizationService
    {
        private string _currentLanguage = "en";

        public string CurrentLanguage => _currentLanguage;

        public event Action? OnLanguageChanged;

        private readonly Dictionary<string, Dictionary<string, string>> _translations = new()
        {
            #region English translations
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
                ["InvoiceSystem"] = "Invoice System",

                // Invoices Page
                ["CreateInvoice"] = "Create Invoice",
                ["EditInvoice"] = "Edit Invoice",
                ["InvoiceDetails"] = "Invoice Details",
                ["AllInvoices"] = "All Invoices",
                ["Receivables"] = "Receivables",
                ["Payables"] = "Payables",
                ["Type"] = "Type",
                ["IssueDate"] = "Issue Date",
                ["Notes"] = "Notes",
                ["LineItems"] = "Line Items",
                ["Description"] = "Description",
                ["Quantity"] = "Quantity",
                ["UnitPrice"] = "Unit Price",
                ["TaxRate"] = "Tax Rate",
                ["Total"] = "Total",
                ["Subtotal"] = "Subtotal",
                ["Tax"] = "Tax",
                ["AmountPaid"] = "Amount Paid",
                ["AmountDue"] = "Amount Due",
                ["AddLineItem"] = "Add Line Item",
                ["RemoveLineItem"] = "Remove",
                ["BillTo"] = "Bill To",
                ["BillFrom"] = "Bill From",
                ["InvoiceInformation"] = "Invoice Information",
                ["PaymentHistory"] = "Payment History",
                ["RecordPayment"] = "Record Payment",
                ["NoPaymentsRecorded"] = "No payments recorded yet",
                ["BackToInvoices"] = "Back to Invoices",
                ["SelectCompany"] = "Select Company",
                ["SelectType"] = "Select Type",
                ["Receivable"] = "Receivable",
                ["Payable"] = "Payable",
                ["ConfirmDelete"] = "Confirm Delete",
                ["DeleteInvoiceConfirm"] = "Are you sure you want to delete this invoice? This action cannot be undone.",
                ["InvoiceDeleted"] = "Invoice deleted successfully",
                ["InvoiceCreated"] = "Invoice created successfully",
                ["InvoiceUpdated"] = "Invoice updated successfully",
                ["ErrorLoadingInvoice"] = "Error loading invoice",
                ["ErrorCreatingInvoice"] = "Error creating invoice",
                ["ErrorUpdatingInvoice"] = "Error updating invoice",
                ["ErrorDeletingInvoice"] = "Error deleting invoice",
                ["PleaseAddLineItems"] = "Please add at least one line item",
                ["Yes"] = "Yes",
                ["No"] = "No",
                ["Close"] = "Close",
                ["PaymentDate"] = "Payment Date",
                ["PaymentAmount"] = "Payment Amount",
                ["PaymentMethod"] = "Payment Method",
                ["ReferenceNumber"] = "Reference Number",
                ["RecordedBy"] = "Recorded By",
                ["ShowingResults"] = "Showing {0} to {1} of {2} results",
                ["Terms"] = "Terms",
                ["Days"] = "days",
                ["SelectInvoiceType"] = "Select Invoice Type",
                ["DaysFromIssue"] = "Days from issue date",
                ["CalculateDueDate"] = "Calculate Due Date",
                ["AddItem"] = "Add Item",
                ["RemoveItem"] = "Remove Item",
                ["Item"] = "Item",
                ["PleaseSelectCompany"] = "Please select a company",
                ["PleaseSelectType"] = "Please select invoice type",
                ["PleaseAddAtLeastOneLineItem"] = "Please add at least one line item",
                ["EnterDescription"] = "Enter description",
                ["EnterQuantity"] = "Enter quantity",
                ["EnterUnitPrice"] = "Enter unit price",
                ["EnterTaxRate"] = "Enter tax rate (%)",

                // Payment Methods
                ["Cash"] = "Cash",
                ["Check"] = "Check",
                ["BankTransfer"] = "Bank Transfer",
                ["CreditCard"] = "Credit Card",
                ["DebitCard"] = "Debit Card",
                ["Other"] = "Other",
            },
            #endregion

            #region Macedonian translations
            ["mk"] = new Dictionary<string, string>
            {
                // Navigation
                ["Dashboard"] = "Почетна",
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
                ["InvoiceSystem"] = "Систем за фактури",

                // Invoices Page
                ["CreateInvoice"] = "Креирај фактура",
                ["EditInvoice"] = "Измени фактура",
                ["InvoiceDetails"] = "Детали за фактура",
                ["AllInvoices"] = "Сите фактури",
                ["Receivables"] = "Побарувања",
                ["Payables"] = "Обврски",
                ["Type"] = "Тип",
                ["IssueDate"] = "Датум на издавање",
                ["Notes"] = "Забелешки",
                ["LineItems"] = "Ставки",
                ["Description"] = "Опис",
                ["Quantity"] = "Количина",
                ["UnitPrice"] = "Единечна цена",
                ["TaxRate"] = "Даночна стапка",
                ["Total"] = "Вкупно",
                ["Subtotal"] = "Подзбир",
                ["Tax"] = "Данок",
                ["AmountPaid"] = "Платено",
                ["AmountDue"] = "За наплата",
                ["AddLineItem"] = "Додади ставка",
                ["RemoveLineItem"] = "Отстрани",
                ["BillTo"] = "Наплата од",
                ["BillFrom"] = "Наплата на",
                ["InvoiceInformation"] = "Информации за фактура",
                ["PaymentHistory"] = "Историја на плаќања",
                ["RecordPayment"] = "Евидентирај плаќање",
                ["NoPaymentsRecorded"] = "Нема евидентирани плаќања",
                ["BackToInvoices"] = "Назад кон фактури",
                ["SelectCompany"] = "Избери компанија",
                ["SelectType"] = "Избери тип",
                ["Receivable"] = "Побарување",
                ["Payable"] = "Обврска",
                ["ConfirmDelete"] = "Потврди бришење",
                ["DeleteInvoiceConfirm"] = "Дали сте сигурни дека сакате да ја избришете оваа фактура? Ова дејство не може да се врати.",
                ["InvoiceDeleted"] = "Фактурата е успешно избришана",
                ["InvoiceCreated"] = "Фактурата е успешно креирана",
                ["InvoiceUpdated"] = "Фактурата е успешно ажурирана",
                ["ErrorLoadingInvoice"] = "Грешка при вчитување на фактура",
                ["ErrorCreatingInvoice"] = "Грешка при креирање на фактура",
                ["ErrorUpdatingInvoice"] = "Грешка при ажурирање на фактура",
                ["ErrorDeletingInvoice"] = "Грешка при бришење на фактура",
                ["PleaseAddLineItems"] = "Ве молиме додадете барем една ставка",
                ["Yes"] = "Да",
                ["No"] = "Не",
                ["Close"] = "Затвори",
                ["PaymentDate"] = "Датум на плаќање",
                ["PaymentAmount"] = "Износ на плаќање",
                ["PaymentMethod"] = "Начин на плаќање",
                ["ReferenceNumber"] = "Референтен број",
                ["RecordedBy"] = "Евидентирано од",
                ["ShowingResults"] = "Прикажани {0} до {1} од {2} резултати",
                ["Terms"] = "Услови",
                ["Days"] = "денови",
                ["SelectInvoiceType"] = "Избери тип на фактура",
                ["DaysFromIssue"] = "Денови од датум на издавање",
                ["CalculateDueDate"] = "Пресметај рок на плаќање",
                ["AddItem"] = "Додади ставка",
                ["RemoveItem"] = "Отстрани ставка",
                ["Item"] = "Ставка",
                ["PleaseSelectCompany"] = "Ве молиме изберете компанија",
                ["PleaseSelectType"] = "Ве молиме изберете тип на фактура",
                ["PleaseAddAtLeastOneLineItem"] = "Ве молиме додадете барем една ставка",
                ["EnterDescription"] = "Внесете опис",
                ["EnterQuantity"] = "Внесете количина",
                ["EnterUnitPrice"] = "Внесете единечна цена",
                ["EnterTaxRate"] = "Внесете даночна стапка (%)",

                // Payment Methods
                ["Cash"] = "Готовина",
                ["Check"] = "Чек",
                ["BankTransfer"] = "Банкарски трансфер",
                ["CreditCard"] = "Кредитна картичка",
                ["DebitCard"] = "Дебитна картичка",
                ["Other"] = "Друго",
            }
            #endregion
        };

        public void SetLanguage(string language)
        {
            if (language != "en" && language != "mk")
                language = "en";

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
