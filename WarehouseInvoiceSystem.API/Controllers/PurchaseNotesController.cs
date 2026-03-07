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
    public class PurchaseNotesController(IPurchaseNoteService purchaseNoteService, ILogger<PurchaseNotesController> logger) : ControllerBase
    {
        /// <summary>
        /// Get all purchase notes
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PurchaseNoteDto>>> GetAll()
        {
            try
            {
                IEnumerable<PurchaseNoteDto> notes = await purchaseNoteService.GetAllPurchaseNotesAsync();
                return Ok(notes);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting all purchase notes");
                return StatusCode(500, "An error occurred while retrieving purchase notes");
            }
        }

        /// <summary>
        /// Get purchase note by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PurchaseNoteDto>> GetById(Guid id)
        {
            try
            {
                PurchaseNoteDto? note = await purchaseNoteService.GetPurchaseNoteByIdAsync(id);
                if (note == null)
                    return NotFound($"Purchase note with ID {id} not found");

                return Ok(note);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting purchase note {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the purchase note");
            }
        }

        /// <summary>
        /// Get purchase note by note number
        /// </summary>
        [HttpGet("number/{noteNumber}")]
        public async Task<ActionResult<PurchaseNoteDto>> GetByNumber(string noteNumber)
        {
            try
            {
                PurchaseNoteDto? note = await purchaseNoteService.GetPurchaseNoteByNumberAsync(noteNumber);
                if (note == null)
                    return NotFound($"Purchase note with number {noteNumber} not found");

                return Ok(note);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting purchase note by number {Number}", noteNumber);
                return StatusCode(500, "An error occurred while retrieving the purchase note");
            }
        }

        /// <summary>
        /// Get purchase notes by individual
        /// </summary>
        [HttpGet("individual/{individualId}")]
        public async Task<ActionResult<IEnumerable<PurchaseNoteDto>>> GetByIndividual(Guid individualId)
        {
            try
            {
                IEnumerable<PurchaseNoteDto> notes = await purchaseNoteService.GetPurchaseNotesByIndividualAsync(individualId);
                return Ok(notes);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting purchase notes for individual {Id}", individualId);
                return StatusCode(500, "An error occurred while retrieving purchase notes");
            }
        }

        /// <summary>
        /// Get purchase notes by date range
        /// </summary>
        [HttpGet("date-range")]
        public async Task<ActionResult<IEnumerable<PurchaseNoteDto>>> GetByDateRange(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                IEnumerable<PurchaseNoteDto> notes = await purchaseNoteService.GetPurchaseNotesByDateRangeAsync(startDate, endDate);
                return Ok(notes);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting purchase notes by date range");
                return StatusCode(500, "An error occurred while retrieving purchase notes");
            }
        }

        /// <summary>
        /// Get purchase notes by status
        /// </summary>
        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<PurchaseNoteDto>>> GetByStatus(PurchaseNoteStatus status)
        {
            try
            {
                IEnumerable<PurchaseNoteDto> notes = await purchaseNoteService.GetPurchaseNotesByStatusAsync(status);
                return Ok(notes);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting purchase notes by status {Status}", status);
                return StatusCode(500, "An error occurred while retrieving purchase notes");
            }
        }

        /// <summary>
        /// Get purchase notes paged with filtering and sorting
        /// </summary>
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<PurchaseNoteDto>>> GetPaged([FromQuery] GetPurchaseNotesQuery query)
        {
            try
            {
                PagedResult<PurchaseNoteDto> result = await purchaseNoteService.GetPagedAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting paged purchase notes");
                return StatusCode(500, "An error occurred while retrieving purchase notes");
            }
        }

        /// <summary>
        /// Get all purchase notes matching filters for export (no pagination)
        /// </summary>
        [HttpGet("export")]
        public async Task<ActionResult<IEnumerable<PurchaseNoteDto>>> GetAllFiltered([FromQuery] GetPurchaseNotesQuery query)
        {
            try
            {
                IEnumerable<PurchaseNoteDto> result = await purchaseNoteService.GetAllFilteredAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting filtered purchase notes for export");
                return StatusCode(500, "An error occurred while retrieving purchase notes");
            }
        }

        /// <summary>
        /// Create new purchase note
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<PurchaseNoteDto>> Create([FromBody] CreatePurchaseNoteDto createDto)
        {
            try
            {
                PurchaseNoteDto note = await purchaseNoteService.CreatePurchaseNoteAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = note.Id }, note);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating purchase note");
                return StatusCode(500, "An error occurred while creating the purchase note");
            }
        }

        /// <summary>
        /// Update purchase note
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<PurchaseNoteDto>> Update(Guid id, [FromBody] UpdatePurchaseNoteDto updateDto)
        {
            try
            {
                PurchaseNoteDto note = await purchaseNoteService.UpdatePurchaseNoteAsync(id, updateDto);
                return Ok(note);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating purchase note {Id}", id);
                return StatusCode(500, "An error occurred while updating the purchase note");
            }
        }

        /// <summary>
        /// Delete purchase note
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                bool deleted = await purchaseNoteService.DeletePurchaseNoteAsync(id);
                if (!deleted)
                    return NotFound($"Purchase note with ID {id} not found");

                return NoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting purchase note {Id}", id);
                return StatusCode(500, "An error occurred while deleting the purchase note");
            }
        }

        /// <summary>
        /// Mark purchase note as paid
        /// </summary>
        [HttpPost("{id}/mark-paid")]
        public async Task<ActionResult<PurchaseNoteDto>> MarkAsPaid(Guid id)
        {
            try
            {
                PurchaseNoteDto note = await purchaseNoteService.MarkAsPaidAsync(id);
                return Ok(note);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error marking purchase note {Id} as paid", id);
                return StatusCode(500, "An error occurred while marking the purchase note as paid");
            }
        }
    }
}