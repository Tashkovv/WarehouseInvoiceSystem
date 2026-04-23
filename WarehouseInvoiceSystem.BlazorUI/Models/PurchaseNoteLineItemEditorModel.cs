using WarehouseInvoiceSystem.Application.DTOs.Product;

namespace WarehouseInvoiceSystem.BlazorUI.Models
{
    public class PurchaseNoteLineItemEditorModel
    {
        public Guid Id { get; set; } = Guid.Empty;
        public ProductDto? SelectedProduct { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal GrossQuantity { get; set; } = 1;
        public decimal KaloPercentage { get; set; } = 0;
        public decimal Quantity => Math.Round(GrossQuantity * (1 - KaloPercentage / 100m), MidpointRounding.AwayFromZero);
        public decimal UnitPrice { get; set; } = 0;
        public decimal LineTotal => Quantity * UnitPrice;
    }
}
