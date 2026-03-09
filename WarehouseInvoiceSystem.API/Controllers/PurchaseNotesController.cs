namespace WarehouseInvoiceSystem.API.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using WarehouseInvoiceSystem.Application.DTOs.PurchaseNote;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    [ApiController]
    [Route("api/[controller]")]
    public class PurchaseNotesController(
        IPurchaseNoteService purchaseNoteService,
        ILogger<PurchaseNotesController> logger) : ControllerBase
    {
        private const string BaseErrorMessage = "An error occurred";
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PurchaseNoteDto>>> GetAll()
        {
            try { return Ok(await purchaseNoteService.GetAllPurchaseNotesAsync()); }
            catch (Exception ex) { logger.LogError(ex, "Error getting all purchase notes"); return StatusCode(500, BaseErrorMessage); }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PurchaseNoteDto>> GetById(Guid id)
        {
            try
            {
                PurchaseNoteDto? note = await purchaseNoteService.GetPurchaseNoteByIdAsync(id);
                return note == null ? NotFound($"Purchase note with ID {id} not found") : Ok(note);
            }
            catch (Exception ex) { logger.LogError(ex, "Error getting purchase note {Id}", id); return StatusCode(500, BaseErrorMessage); }
        }

        [HttpGet("number/{noteNumber}")]
        public async Task<ActionResult<PurchaseNoteDto>> GetByNumber(string noteNumber)
        {
            try
            {
                PurchaseNoteDto? note = await purchaseNoteService.GetPurchaseNoteByNumberAsync(noteNumber);
                return note == null ? NotFound($"Purchase note with number {noteNumber} not found") : Ok(note);
            }
            catch (Exception ex) { logger.LogError(ex, "Error getting purchase note by number {Number}", noteNumber); return StatusCode(500, BaseErrorMessage); }
        }

        [HttpGet("individual/{individualId}")]
        public async Task<ActionResult<IEnumerable<PurchaseNoteDto>>> GetByIndividual(Guid individualId)
        {
            try { return Ok(await purchaseNoteService.GetPurchaseNotesByIndividualAsync(individualId)); }
            catch (Exception ex) { logger.LogError(ex, "Error getting purchase notes for individual {Id}", individualId); return StatusCode(500, BaseErrorMessage); }
        }

        [HttpGet("date-range")]
        public async Task<ActionResult<IEnumerable<PurchaseNoteDto>>> GetByDateRange(
            [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try { return Ok(await purchaseNoteService.GetPurchaseNotesByDateRangeAsync(startDate, endDate)); }
            catch (Exception ex) { logger.LogError(ex, "Error getting purchase notes by date range"); return StatusCode(500, BaseErrorMessage); }
        }

        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<PurchaseNoteDto>>> GetByStatus(PurchaseNoteStatus status)
        {
            try { return Ok(await purchaseNoteService.GetPurchaseNotesByStatusAsync(status)); }
            catch (Exception ex) { logger.LogError(ex, "Error getting purchase notes by status {Status}", status); return StatusCode(500, BaseErrorMessage); }
        }

        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<PurchaseNoteDto>>> GetPaged([FromQuery] GetPurchaseNotesQuery query)
        {
            try { return Ok(await purchaseNoteService.GetPagedAsync(query)); }
            catch (Exception ex) { logger.LogError(ex, "Error getting paged purchase notes"); return StatusCode(500, BaseErrorMessage); }
        }

        [HttpGet("export")]
        public async Task<ActionResult<IEnumerable<PurchaseNoteDto>>> GetAllFiltered([FromQuery] GetPurchaseNotesQuery query)
        {
            try { return Ok(await purchaseNoteService.GetAllFilteredAsync(query)); }
            catch (Exception ex) { logger.LogError(ex, "Error getting filtered purchase notes"); return StatusCode(500, BaseErrorMessage); }
        }

        [HttpPost]
        public async Task<ActionResult<PurchaseNoteDto>> Create([FromBody] CreatePurchaseNoteDto createDto)
        {
            try
            {
                PurchaseNoteDto note = await purchaseNoteService.CreatePurchaseNoteAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = note.Id }, note);
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { logger.LogError(ex, "Error creating purchase note"); return StatusCode(500, BaseErrorMessage); }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<PurchaseNoteDto>> Update(Guid id, [FromBody] UpdatePurchaseNoteDto updateDto)
        {
            try { return Ok(await purchaseNoteService.UpdatePurchaseNoteAsync(id, updateDto)); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { logger.LogError(ex, "Error updating purchase note {Id}", id); return StatusCode(500, BaseErrorMessage); }
        }

        /// <summary>Draft → Pending. Creates inventory transactions.</summary>
        [HttpPost("{id}/receive")]
        public async Task<ActionResult<PurchaseNoteDto>> Receive(Guid id)
        {
            try { return Ok(await purchaseNoteService.ReceiveAsync(id)); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { logger.LogError(ex, "Error receiving purchase note {Id}", id); return StatusCode(500, BaseErrorMessage); }
        }

        /// <summary>Pending → Paid.</summary>
        [HttpPost("{id}/mark-paid")]
        public async Task<ActionResult<PurchaseNoteDto>> MarkAsPaid(Guid id)
        {
            try { return Ok(await purchaseNoteService.MarkAsPaidAsync(id)); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { logger.LogError(ex, "Error marking purchase note {Id} as paid", id); return StatusCode(500, BaseErrorMessage); }
        }

        /// <summary>Pending → Draft. Reverses inventory transactions.</summary>
        [HttpPost("{id}/revert-to-draft")]
        public async Task<ActionResult<PurchaseNoteDto>> RevertToDraft(Guid id)
        {
            try { return Ok(await purchaseNoteService.RevertToDraftAsync(id)); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { logger.LogError(ex, "Error reverting purchase note {Id} to draft", id); return StatusCode(500, BaseErrorMessage); }
        }

        /// <summary>Draft or Pending → Cancelled. Reverses stock if was Pending.</summary>
        [HttpPost("{id}/cancel")]
        public async Task<ActionResult<PurchaseNoteDto>> Cancel(Guid id)
        {
            try { return Ok(await purchaseNoteService.CancelAsync(id)); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { logger.LogError(ex, "Error cancelling purchase note {Id}", id); return StatusCode(500, BaseErrorMessage); }
        }
    }
}
