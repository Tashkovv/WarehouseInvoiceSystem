namespace WarehouseInvoiceSystem.Application.Models
{
    public record ExportColumn<T>(
        string Header,
        Func<T, object?> Selector,
        ExportColumnType ColumnType = ExportColumnType.Text,
        bool IncludeInTotals = false
    );

    public enum ExportColumnType { Text, Currency, Date, Number }

    public record ExportListOptions(
        string SheetName,
        string? Title = null,
        IReadOnlyList<string>? SubtitleLines = null
    );
}
