namespace WarehouseInvoiceSystem.API.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using WarehouseInvoiceSystem.Application.DTOs.Invoice;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    [ApiController]
    [Route("api/[controller]")]
    public class InvoicesController(IInvoiceService invoiceService,
                                    ILogger<InvoicesController> logger) : ControllerBase
    {
        /// <summary>
        /// Get all invoices
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetAll()
        {
            try
            {
                IEnumerable<InvoiceDto> invoices = await invoiceService.GetAllInvoicesAsync();
                return Ok(invoices);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting all invoices");
                return StatusCode(500, "An error occurred while retrieving invoices");
            }
        }

        /// <summary>
        /// Get invoice by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<InvoiceDto>> GetById(Guid id)
        {
            try
            {
                InvoiceDto? invoice = await invoiceService.GetInvoiceByIdAsync(id);
                if (invoice == null)
                    return NotFound($"Invoice with ID {id} not found");

                return Ok(invoice);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting invoice {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the invoice");
            }
        }

        /// <summary>
        /// Get invoice by invoice number
        /// </summary>
        [HttpGet("number/{invoiceNumber}")]
        public async Task<ActionResult<InvoiceDto>> GetByNumber(string invoiceNumber)
        {
            try
            {
                InvoiceDto? invoice = await invoiceService.GetInvoiceByNumberAsync(invoiceNumber);
                if (invoice == null)
                    return NotFound($"Invoice with number {invoiceNumber} not found");

                return Ok(invoice);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting invoice by number {Number}", invoiceNumber);
                return StatusCode(500, "An error occurred while retrieving the invoice");
            }
        }

        /// <summary>
        /// Get invoices by company ID
        /// </summary>
        [HttpGet("company/{companyId}")]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetByCompany(Guid companyId)
        {
            try
            {
                IEnumerable<InvoiceDto> invoices = await invoiceService.GetInvoicesByCompanyAsync(companyId);
                return Ok(invoices);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting invoices for company {CompanyId}", companyId);
                return StatusCode(500, "An error occurred while retrieving invoices");
            }
        }

        /// <summary>
        /// Get invoices by type (Receivable or Payable)
        /// </summary>
        [HttpGet("type/{type}")]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetByType(InvoiceType type)
        {
            try
            {
                IEnumerable<InvoiceDto> invoices = await invoiceService.GetInvoicesByTypeAsync(type);
                return Ok(invoices);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting invoices by type {Type}", type);
                return StatusCode(500, "An error occurred while retrieving invoices");
            }
        }

        /// <summary>
        /// Get invoices by status
        /// </summary>
        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetByStatus(InvoiceStatus status)
        {
            try
            {
                IEnumerable<InvoiceDto> invoices = await invoiceService.GetInvoicesByStatusAsync(status);
                return Ok(invoices);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting invoices by status {Status}", status);
                return StatusCode(500, "An error occurred while retrieving invoices");
            }
        }

        /// <summary>
        /// Get overdue invoices
        /// </summary>
        [HttpGet("overdue")]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetOverdue()
        {
            try
            {
                IEnumerable<InvoiceDto> invoices = await invoiceService.GetOverdueInvoicesAsync();
                return Ok(invoices);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting overdue invoices");
                return StatusCode(500, "An error occurred while retrieving overdue invoices");
            }
        }

        /// <summary>
        /// Get invoice summary statistics
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult<InvoiceSummaryDto>> GetSummary()
        {
            try
            {
                InvoiceSummaryDto summary = await invoiceService.GetPayableInvoiceSummaryAsync();
                return Ok(summary);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting invoice summary");
                return StatusCode(500, "An error occurred while retrieving invoice summary");
            }
        }

        /// <summary>
        /// Get invoices paged with filtering and sorting
        /// </summary>
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<InvoiceDto>>> GetPaged([FromQuery] GetInvoicesQuery query)
        {
            try
            {
                PagedResult<InvoiceDto> result = await invoiceService.GetPagedAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting paged invoices");
                return StatusCode(500, "An error occurred while retrieving invoices");
            }
        }

        /// <summary>
        /// Get all invoices matching filters for export (no pagination)
        /// </summary>
        [HttpGet("export")]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetAllFiltered([FromQuery] GetInvoicesQuery query)
        {
            try
            {
                IEnumerable<InvoiceDto> result = await invoiceService.GetAllFilteredAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting filtered invoices for export");
                return StatusCode(500, "An error occurred while retrieving invoices");
            }
        }

        /// <summary>
        /// Create a new invoice
        /// </summary>s
        [HttpPost]
        public async Task<ActionResult<InvoiceDto>> Create([FromBody] CreateInvoiceDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                InvoiceDto invoice = await invoiceService.CreateInvoiceAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating invoice");
                return StatusCode(500, "An error occurred while creating the invoice");
            }
        }

        /// <summary>
        /// Update an existing invoice
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<InvoiceDto>> Update(Guid id, [FromBody] UpdateInvoiceDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                InvoiceDto invoice = await invoiceService.UpdateInvoiceAsync(id, updateDto);
                return Ok(invoice);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating invoice {Id}", id);
                return StatusCode(500, "An error occurred while updating the invoice");
            }
        }

        /// <summary>
        /// Delete an invoice
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                bool result = await invoiceService.DeleteInvoiceAsync(id);
                if (!result)
                    return NotFound($"Invoice with ID {id} not found");

                return NoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting invoice {Id}", id);
                return StatusCode(500, "An error occurred while deleting the invoice");
            }
        }
    }
}