namespace WarehouseInvoiceSystem.BlazorUI.Localization
{
    using Microsoft.Extensions.Localization;
    using MudBlazor;
    using WarehouseInvoiceSystem.Application.Interfaces;

    public class CustomMudLocalizer(ILocalizationService localization) : MudLocalizer
    {
        public override LocalizedString this[string key] =>
            key switch
            {
                "MudDataGridPager_RowsPerPage" => new LocalizedString(key, localization.GetString("ItemsPerPage")),
                "MudDataGridPager_AllItems" => new LocalizedString(key, localization.GetString("AllItems")),
                _ => new LocalizedString(key, key, resourceNotFound: true)
            };
    }
}
