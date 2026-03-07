namespace WarehouseInvoiceSystem.API.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using WarehouseInvoiceSystem.Application.DTOs.InventoryTransaction;
    using WarehouseInvoiceSystem.Application.DTOs.StockLevel;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Application.Models;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController(IInventoryService inventoryService, ILogger<InventoryController> logger) : ControllerBase
    {
        #region Stock Levels

        /// <summary>
        /// Get stock level for specific product and warehouse
        /// </summary>
        [HttpGet("stock/{productId}/{warehouseId}")]
        public async Task<ActionResult<StockLevelDto>> GetStockLevel(Guid productId, Guid warehouseId)
        {
            try
            {
                StockLevelDto? stockLevel = await inventoryService.GetStockLevelAsync(productId, warehouseId);
                if (stockLevel == null)
                    return NotFound($"No stock found for product {productId} in warehouse {warehouseId}");

                return Ok(stockLevel);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting stock level");
                return StatusCode(500, "An error occurred while retrieving stock level");
            }
        }

        /// <summary>
        /// Get all stock levels for a product across all warehouses
        /// </summary>
        [HttpGet("stock/product/{productId}")]
        public async Task<ActionResult<IEnumerable<StockLevelDto>>> GetStockByProduct(Guid productId)
        {
            try
            {
                IEnumerable<StockLevelDto> stockLevels = await inventoryService.GetStockByProductAsync(productId);
                return Ok(stockLevels);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting stock by product {ProductId}", productId);
                return StatusCode(500, "An error occurred while retrieving stock levels");
            }
        }

        /// <summary>
        /// Get all stock levels in a warehouse
        /// </summary>
        [HttpGet("stock/warehouse/{warehouseId}")]
        public async Task<ActionResult<IEnumerable<StockLevelDto>>> GetStockByWarehouse(Guid warehouseId)
        {
            try
            {
                IEnumerable<StockLevelDto> stockLevels = await inventoryService.GetStockByWarehouseAsync(warehouseId);
                return Ok(stockLevels);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting stock by warehouse {WarehouseId}", warehouseId);
                return StatusCode(500, "An error occurred while retrieving stock levels");
            }
        }

        /// <summary>
        /// Get low stock items (below minimum quantity)
        /// </summary>
        [HttpGet("stock/low")]
        public async Task<ActionResult<IEnumerable<StockLevelDto>>> GetLowStock([FromQuery] Guid? warehouseId = null)
        {
            try
            {
                IEnumerable<StockLevelDto> stockLevels = await inventoryService.GetLowStockItemsAsync(warehouseId);
                return Ok(stockLevels);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting low stock items");
                return StatusCode(500, "An error occurred while retrieving low stock items");
            }
        }

        /// <summary>
        /// Get stock levels paged with filtering and sorting
        /// </summary>
        [HttpGet("stock/paged")]
        public async Task<ActionResult<PagedResult<StockLevelDto>>> GetStockPaged([FromQuery] GetStockQuery query)
        {
            try
            {
                PagedResult<StockLevelDto> result = await inventoryService.GetPagedStockAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting paged stock levels");
                return StatusCode(500, "An error occurred while retrieving stock levels");
            }
        }

        /// <summary>
        /// Update stock level settings (minimum, reorder point)
        /// </summary>
        [HttpPut("stock/{productId}/{warehouseId}")]
        public async Task<ActionResult<StockLevelDto>> UpdateStockLevel(
            Guid productId,
            Guid warehouseId,
            [FromBody] UpdateStockLevelDto updateDto)
        {
            try
            {
                StockLevelDto stockLevel = await inventoryService.UpdateStockLevelAsync(productId, warehouseId, updateDto);
                return Ok(stockLevel);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating stock level");
                return StatusCode(500, "An error occurred while updating stock level");
            }
        }

        #endregion

        #region Transactions

        /// <summary>
        /// Get all inventory transactions
        /// </summary>
        [HttpGet("transactions")]
        public async Task<ActionResult<IEnumerable<InventoryTransactionDto>>> GetAllTransactions()
        {
            try
            {
                IEnumerable<InventoryTransactionDto> transactions = await inventoryService.GetAllTransactionsAsync();
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting all transactions");
                return StatusCode(500, "An error occurred while retrieving transactions");
            }
        }

        /// <summary>
        /// Get transactions for a specific product
        /// </summary>
        [HttpGet("transactions/product/{productId}")]
        public async Task<ActionResult<IEnumerable<InventoryTransactionDto>>> GetTransactionsByProduct(Guid productId)
        {
            try
            {
                IEnumerable<InventoryTransactionDto> transactions = await inventoryService.GetTransactionsByProductAsync(productId);
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting transactions for product {ProductId}", productId);
                return StatusCode(500, "An error occurred while retrieving transactions");
            }
        }

        /// <summary>
        /// Get transactions for a specific warehouse
        /// </summary>
        [HttpGet("transactions/warehouse/{warehouseId}")]
        public async Task<ActionResult<IEnumerable<InventoryTransactionDto>>> GetTransactionsByWarehouse(Guid warehouseId)
        {
            try
            {
                IEnumerable<InventoryTransactionDto> transactions = await inventoryService.GetTransactionsByWarehouseAsync(warehouseId);
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting transactions for warehouse {WarehouseId}", warehouseId);
                return StatusCode(500, "An error occurred while retrieving transactions");
            }
        }

        /// <summary>
        /// Create inventory transaction (manual stock movement)
        /// </summary>
        [HttpPost("transactions")]
        public async Task<ActionResult<InventoryTransactionDto>> CreateTransaction([FromBody] CreateInventoryTransactionDto createDto)
        {
            try
            {
                InventoryTransactionDto transaction = await inventoryService.CreateTransactionAsync(createDto);
                return Ok(transaction);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating transaction");
                return StatusCode(500, "An error occurred while creating the transaction");
            }
        }

        /// <summary>
        /// Adjust stock (quick manual adjustment)
        /// </summary>
        [HttpPost("adjust")]
        public async Task<ActionResult> AdjustStock([FromBody] AdjustStockRequest request)
        {
            try
            {
                await inventoryService.AdjustStockAsync(
                    request.ProductId,
                    request.WarehouseId,
                    request.QuantityChange,
                    request.Reason);
                return Ok();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adjusting stock");
                return StatusCode(500, "An error occurred while adjusting stock");
            }
        }

        #endregion
    }
}