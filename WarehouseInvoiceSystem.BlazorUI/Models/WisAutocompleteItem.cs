namespace WarehouseInvoiceSystem.BlazorUI.Models
{
    /// <summary>
    /// A single selectable item used by <see cref="Components.Shared.WisAutocompleteFilter{TValue}"/>.
    /// </summary>
    public class WisAutocompleteItem<TValue>
    {
        /// <summary>The primary label shown in the list and on the trigger button when selected.</summary>
        public required string Label { get; set; }

        /// <summary>Optional secondary label shown dimmed alongside the primary label.</summary>
        public string? SubLabel { get; set; }

        /// <summary>The value emitted via SelectedValueChanged when this item is picked.</summary>
        public required TValue Value { get; set; }
    }
}
