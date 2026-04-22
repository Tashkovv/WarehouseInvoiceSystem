namespace WarehouseInvoiceSystem.BlazorUI.Models
{
    using WarehouseInvoiceSystem.Application.DTOs.Product;

    public class LineItemEditorModel
    {
        /// <summary>Guid.Empty means this is a new (unsaved) line item.</summary>
        public Guid Id { get; set; } = Guid.Empty;
        public ProductDto? SelectedProduct { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; } = 0;
        public decimal TaxRate { get; set; } = 0;
        public decimal DiscountPercentage { get; set; } = 0;

        public decimal LineTotal
        {
            get
            {
                decimal net = (Quantity * UnitPrice) * (1 - DiscountPercentage / 100m);
                return net + net * (TaxRate / 100m);
            }
        }
    }
}
