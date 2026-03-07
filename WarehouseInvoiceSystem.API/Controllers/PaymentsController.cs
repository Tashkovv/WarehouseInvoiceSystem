namespace WarehouseInvoiceSystem.API.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using WarehouseInvoiceSystem.Application.DTOs.Payment;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController(
        IPaymentService paymentService,
        ILogger<PaymentsController> logger) : ControllerBase
    {
        /// <summary>
        /// Get all payments
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PaymentDto>>> GetAll()
        {
            try
            {
                IEnumerable<PaymentDto> payments = await paymentService.GetAllPaymentsAsync();
                return Ok(payments);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting all payments");
                return StatusCode(500, "An error occurred while retrieving payments");
            }
        }

        /// <summary>
        /// Get payment by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PaymentDto>> GetById(Guid id)
        {
            try
            {
                PaymentDto? payment = await paymentService.GetPaymentByIdAsync(id);
                if (payment == null)
                    return NotFound($"Payment with ID {id} not found");

                return Ok(payment);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting payment {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the payment");
            }
        }

        /// <summary>
        /// Get all payments for a specific invoice
        /// </summary>
        [HttpGet("invoice/{invoiceId}")]
        public async Task<ActionResult<IEnumerable<PaymentDto>>> GetByInvoice(Guid invoiceId)
        {
            try
            {
                IEnumerable<PaymentDto> payments = await paymentService.GetPaymentsByInvoiceAsync(invoiceId);
                return Ok(payments);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting payments for invoice {InvoiceId}", invoiceId);
                return StatusCode(500, "An error occurred while retrieving payments");
            }
        }

        /// <summary>
        /// Get payments paged with filtering and sorting
        /// </summary>
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<PaymentDto>>> GetPaged([FromQuery] GetPaymentsQuery query)
        {
            try
            {
                PagedResult<PaymentDto> result = await paymentService.GetPagedAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting paged payments");
                return StatusCode(500, "An error occurred while retrieving payments");
            }
        }

        /// <summary>
        /// Record a new payment
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<PaymentDto>> Create([FromBody] CreatePaymentDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                PaymentDto payment = await paymentService.CreatePaymentAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = payment.Id }, payment);
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
                logger.LogError(ex, "Error creating payment");
                return StatusCode(500, "An error occurred while creating the payment");
            }
        }

        /// <summary>
        /// Update an existing payment
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<PaymentDto>> Update(Guid id, [FromBody] UpdatePaymentDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                PaymentDto payment = await paymentService.UpdatePaymentAsync(id, updateDto);
                return Ok(payment);
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
                logger.LogError(ex, "Error updating payment {Id}", id);
                return StatusCode(500, "An error occurred while updating the payment");
            }
        }

        /// <summary>
        /// Delete a payment
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                bool result = await paymentService.DeletePaymentAsync(id);
                if (!result)
                    return NotFound($"Payment with ID {id} not found");

                return NoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting payment {Id}", id);
                return StatusCode(500, "An error occurred while deleting the payment");
            }
        }
    }
}
