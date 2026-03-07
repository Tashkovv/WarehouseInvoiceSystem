namespace WarehouseInvoiceSystem.BlazorUI.Models
{
    using MudBlazor;

    public class FilterPopoverItem<TValue>
    {
        public string Label { get; set; } = string.Empty;
        public TValue Value { get; set; } = default!;
        public Color Color { get; set; } = Color.Default;
    }

    public enum FilterSelectionMode
    {
        Single,
        Multiple
    }
}
