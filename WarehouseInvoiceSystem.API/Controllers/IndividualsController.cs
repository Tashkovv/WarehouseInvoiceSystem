namespace WarehouseInvoiceSystem.API.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using WarehouseInvoiceSystem.Application.DTOs.Individual;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    [ApiController]
    [Route("api/[controller]")]
    public class IndividualsController(IIndividualService individualService, ILogger<IndividualsController> logger) : ControllerBase
    {
        /// <summary>
        /// Get all individuals
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<IndividualDto>>> GetAll()
        {
            try
            {
                IEnumerable<IndividualDto> individuals = await individualService.GetAllIndividualsAsync();
                return Ok(individuals);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting all individuals");
                return StatusCode(500, "An error occurred while retrieving individuals");
            }
        }

        /// <summary>
        /// Get active individuals only
        /// </summary>
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<IndividualDto>>> GetActive()
        {
            try
            {
                IEnumerable<IndividualDto> individuals = await individualService.GetActiveIndividualsAsync();
                return Ok(individuals);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting active individuals");
                return StatusCode(500, "An error occurred while retrieving individuals");
            }
        }

        /// <summary>
        /// Get individuals paged with filtering and sorting
        /// </summary>
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<IndividualDto>>> GetPaged([FromQuery] GetIndividualsQuery query)
        {
            try
            {
                PagedResult<IndividualDto> result = await individualService.GetPagedAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting paged individuals");
                return StatusCode(500, "An error occurred while retrieving individuals");
            }
        }

        /// <summary>
        /// Get individual by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<IndividualDto>> GetById(Guid id)
        {
            try
            {
                IndividualDto? individual = await individualService.GetIndividualByIdAsync(id);
                if (individual == null)
                    return NotFound($"Individual with ID {id} not found");

                return Ok(individual);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting individual {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the individual");
            }
        }

        /// <summary>
        /// Get individual by identification number
        /// </summary>
        [HttpGet("identification/{identificationNumber}")]
        public async Task<ActionResult<IndividualDto>> GetByIdentificationNumber(string identificationNumber)
        {
            try
            {
                IndividualDto? individual = await individualService.GetIndividualByIdentificationNumberAsync(identificationNumber);
                if (individual == null)
                    return NotFound($"Individual with identification number {identificationNumber} not found");

                return Ok(individual);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting individual by identification number {Number}", identificationNumber);
                return StatusCode(500, "An error occurred while retrieving the individual");
            }
        }

        /// <summary>
        /// Create new individual
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<IndividualDto>> Create([FromBody] CreateIndividualDto createDto)
        {
            try
            {
                IndividualDto individual = await individualService.CreateIndividualAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = individual.Id }, individual);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating individual");
                return StatusCode(500, "An error occurred while creating the individual");
            }
        }

        /// <summary>
        /// Update individual
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<IndividualDto>> Update(Guid id, [FromBody] UpdateIndividualDto updateDto)
        {
            try
            {
                IndividualDto individual = await individualService.UpdateIndividualAsync(id, updateDto);
                return Ok(individual);
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
                logger.LogError(ex, "Error updating individual {Id}", id);
                return StatusCode(500, "An error occurred while updating the individual");
            }
        }

        /// <summary>
        /// Delete individual
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                bool deleted = await individualService.DeleteIndividualAsync(id);
                if (!deleted)
                    return NotFound($"Individual with ID {id} not found");

                return NoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting individual {Id}", id);
                return StatusCode(500, "An error occurred while deleting the individual");
            }
        }
    }
}