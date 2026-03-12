namespace WarehouseInvoiceSystem.BlazorUI.Models
{
    using MudBlazor;
    using Microsoft.AspNetCore.Components;

    public class WisDetailActionItem
    {
        public string Icon { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public EventCallback OnClick { get; set; }
        public bool Disabled { get; set; } = false;
        public string? DisabledReason { get; set; }
        public bool IsDividerBefore { get; set; } = false;
        public Color Color { get; set; } = Color.Default;
    }
}
