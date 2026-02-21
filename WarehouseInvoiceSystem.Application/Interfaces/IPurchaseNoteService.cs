namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.PurchaseNote;
    using WarehouseInvoiceSystem.Domain.Enums;

    public interface IPurchaseNoteService
    {
        Task<IEnumerable<PurchaseNoteDto>> GetAllPurchaseNotesAsync();
        Task<PurchaseNoteDto?> GetPurchaseNoteByIdAsync(Guid id);
        Task<PurchaseNoteDto?> GetPurchaseNoteByNumberAsync(string noteNumber);
        Task<IEnumerable<PurchaseNoteDto>> GetPurchaseNotesByIndividualAsync(Guid individualId);
        Task<IEnumerable<PurchaseNoteDto>> GetPurchaseNotesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<PurchaseNoteDto>> GetPurchaseNotesByStatusAsync(PurchaseNoteStatus status);
        Task<PurchaseNoteDto> CreatePurchaseNoteAsync(CreatePurchaseNoteDto createDto);
        Task<PurchaseNoteDto> UpdatePurchaseNoteAsync(Guid id, UpdatePurchaseNoteDto updateDto);
        Task<bool> DeletePurchaseNoteAsync(Guid id);
        Task<PurchaseNoteDto> MarkAsPaidAsync(Guid id);
    }
}