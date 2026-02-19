namespace WarehouseInvoiceSystem.API.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using WarehouseInvoiceSystem.Application.DTOs.Product;
    using WarehouseInvoiceSystem.Application.Interfaces;

    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController(IProductService productService, ILogger<ProductsController> logger) : ControllerBase
    {
        /// <summary>
        /// Get all products
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
        {
            try
            {
                IEnumerable<ProductDto> products = await productService.GetAllProductsAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting all products");
                return StatusCode(500, "An error occurred while retrieving products");
            }
        }

        /// <summary>
        /// Get active products only
        /// </summary>
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetActive()
        {
            try
            {
                IEnumerable<ProductDto> products = await productService.GetActiveProductsAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting active products");
                return StatusCode(500, "An error occurred while retrieving products");
            }
        }

        /// <summary>
        /// Get product by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetById(Guid id)
        {
            try
            {
                ProductDto? product = await productService.GetProductByIdAsync(id);
                if (product == null)
                    return NotFound($"Product with ID {id} not found");

                return Ok(product);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting product {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the product");
            }
        }

        /// <summary>
        /// Get product by code
        /// </summary>
        [HttpGet("code/{code}")]
        public async Task<ActionResult<ProductDto>> GetByCode(string code)
        {
            try
            {
                ProductDto? product = await productService.GetProductByCodeAsync(code);
                if (product == null)
                    return NotFound($"Product with code {code} not found");

                return Ok(product);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting product by code {Code}", code);
                return StatusCode(500, "An error occurred while retrieving the product");
            }
        }

        /// <summary>
        /// Create new product
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto createDto)
        {
            try
            {
                ProductDto product = await productService.CreateProductAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating product");
                return StatusCode(500, "An error occurred while creating the product");
            }
        }

        /// <summary>
        /// Update product
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ProductDto>> Update(Guid id, [FromBody] UpdateProductDto updateDto)
        {
            try
            {
                ProductDto product = await productService.UpdateProductAsync(id, updateDto);
                return Ok(product);
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
                logger.LogError(ex, "Error updating product {Id}", id);
                return StatusCode(500, "An error occurred while updating the product");
            }
        }

        /// <summary>
        /// Delete product
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                bool deleted = await productService.DeleteProductAsync(id);
                if (!deleted)
                    return NotFound($"Product with ID {id} not found");

                return NoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting product {Id}", id);
                return StatusCode(500, "An error occurred while deleting the product");
            }
        }
    }
}