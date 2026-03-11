namespace WarehouseInvoiceSystem.API.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using WarehouseInvoiceSystem.Application.DTOs.Warehouse;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    [ApiController]
    [Route("api/[controller]")]
    public class WarehousesController(IWarehouseService warehouseService, ILogger<WarehousesController> logger) : ControllerBase
    {
        /// <summary>
        /// Get all warehouses
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WarehouseDto>>> GetAll()
        {
            try
            {
                IEnumerable<WarehouseDto> warehouses = await warehouseService.GetAllWarehousesAsync();
                return Ok(warehouses);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting all warehouses");
                return StatusCode(500, "An error occurred while retrieving warehouses");
            }
        }

        /// <summary>
        /// Get warehouse by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<WarehouseDto>> GetById(Guid id)
        {
            try
            {
                WarehouseDto? warehouse = await warehouseService.GetWarehouseByIdAsync(id);
                if (warehouse == null)
                    return NotFound($"Warehouse with ID {id} not found");

                return Ok(warehouse);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting warehouse {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the warehouse");
            }
        }

        /// <summary>
        /// Get default warehouse
        /// </summary>
        [HttpGet("default")]
        public async Task<ActionResult<WarehouseDto>> GetDefault()
        {
            try
            {
                WarehouseDto? warehouse = await warehouseService.GetDefaultWarehouseAsync();
                if (warehouse == null)
                    return NotFound("No default warehouse found");

                return Ok(warehouse);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting default warehouse");
                return StatusCode(500, "An error occurred while retrieving the default warehouse");
            }
        }

        /// <summary>
        /// Get warehouses paged with filtering and sorting
        /// </summary>
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<WarehouseDto>>> GetPaged([FromQuery] GetWarehousesQuery query)
        {
            try
            {
                PagedResult<WarehouseDto> result = await warehouseService.GetPagedAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting paged warehouses");
                return StatusCode(500, "An error occurred while retrieving warehouses");
            }
        }

        /// <summary>
        /// Create new warehouse
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<WarehouseDto>> Create([FromBody] CreateWarehouseDto createDto)
        {
            try
            {
                WarehouseDto warehouse = await warehouseService.CreateWarehouseAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = warehouse.Id }, warehouse);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating warehouse");
                return StatusCode(500, "An error occurred while creating the warehouse");
            }
        }

        /// <summary>
        /// Update warehouse
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<WarehouseDto>> Update(Guid id, [FromBody] UpdateWarehouseDto updateDto)
        {
            try
            {
                WarehouseDto warehouse = await warehouseService.UpdateWarehouseAsync(id, updateDto);
                return Ok(warehouse);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating warehouse {Id}", id);
                return StatusCode(500, "An error occurred while updating the warehouse");
            }
        }
    }
}