namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.DTOs.Product;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;

    public class ProductService(IProductRepository productRepository) : IProductService
    {
        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
        {
            IEnumerable<Product> products = await productRepository.GetAllAsync();
            return products.Select(MapToDto);
        }

        public async Task<IEnumerable<ProductDto>> GetActiveProductsAsync()
        {
            IEnumerable<Product> products = await productRepository.GetActiveProductsAsync();
            return products.Select(MapToDto);
        }

        public async Task<ProductDto?> GetProductByIdAsync(Guid id)
        {
            Product? product = await productRepository.GetByIdAsync(id);
            return product == null ? null : MapToDto(product);
        }

        public async Task<ProductDto?> GetProductByCodeAsync(string code)
        {
            Product? product = await productRepository.GetByCodeAsync(code);
            return product == null ? null : MapToDto(product);
        }

        public async Task<ProductDto> CreateProductAsync(CreateProductDto createDto)
        {
            // Validate unique code
            if (await productRepository.CodeExistsAsync(createDto.Code))
                throw new InvalidOperationException($"Product with code '{createDto.Code}' already exists");

            Product product = new()
            {
                Code = createDto.Code,
                Name = createDto.Name,
                Description = createDto.Description,
                Unit = createDto.Unit,
                DefaultPrice = createDto.DefaultPrice,
                IsActive = createDto.IsActive
            };

            Product created = await productRepository.CreateAsync(product);
            return MapToDto(created);
        }

        public async Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductDto updateDto)
        {
            Product? product = await productRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Product with ID {id} not found");

            // Validate unique code
            if (await productRepository.CodeExistsAsync(updateDto.Code, id))
                throw new InvalidOperationException($"Product with code '{updateDto.Code}' already exists");

            product.Code = updateDto.Code;
            product.Name = updateDto.Name;
            product.Description = updateDto.Description;
            product.Unit = updateDto.Unit;
            product.DefaultPrice = updateDto.DefaultPrice;
            product.IsActive = updateDto.IsActive;

            Product updated = await productRepository.UpdateAsync(product);
            return MapToDto(updated);
        }

        public async Task<bool> DeleteProductAsync(Guid id)
        {
            return await productRepository.DeleteAsync(id);
        }

        private static ProductDto MapToDto(Product product)
        {
            return new ProductDto
            {
                Id = product.Id,
                Code = product.Code,
                Name = product.Name,
                Description = product.Description,
                Unit = product.Unit,
                DefaultPrice = product.DefaultPrice,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt
            };
        }
    }
}