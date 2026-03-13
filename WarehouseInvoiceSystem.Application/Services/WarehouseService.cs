namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.DTOs.Warehouse;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public class WarehouseService(IWarehouseRepository warehouseRepository) : IWarehouseService
    {
        public async Task<IEnumerable<WarehouseDto>> GetAllWarehousesAsync()
        {
            IEnumerable<Warehouse> warehouses = await warehouseRepository.GetAllAsync();
            return warehouses.Select(x => MapToDto(x));
        }

        public async Task<PagedResult<WarehouseDto>> GetPagedAsync(GetWarehousesQuery query)
        {
            PagedResult<Warehouse> result = await warehouseRepository.GetPagedAsync(query);

            // Check HasProducts for each warehouse in the page
            List<WarehouseDto> items = [];
            foreach (Warehouse w in result.Items)
            {
                bool hasProducts = await warehouseRepository.HasProductsAsync(w.Id);
                items.Add(MapToDto(w, hasProducts));
            }

            return new PagedResult<WarehouseDto>
            {
                Items = items,
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<WarehouseDto?> GetWarehouseByIdAsync(Guid id)
        {
            Warehouse? warehouse = await warehouseRepository.GetByIdAsync(id);
            return warehouse == null ? null : MapToDto(warehouse);
        }

        public async Task<WarehouseDto?> GetDefaultWarehouseAsync()
        {
            Warehouse? warehouse = await warehouseRepository.GetDefaultWarehouseAsync();
            return warehouse == null ? null : MapToDto(warehouse);
        }

        public async Task<bool> SetDefaultWarehouseAsync(Guid id)
        {
            Warehouse? defaultWarehouse = await warehouseRepository.GetDefaultWarehouseAsync();
            if (defaultWarehouse == null)
            {
                return false;
            }

            if (defaultWarehouse.Id == id)
            {
                return true;
            }

            defaultWarehouse.IsDefault = false;
            await warehouseRepository.UpdateAsync(defaultWarehouse);

            Warehouse? newDefaultWarehouse = await warehouseRepository.GetByIdAsync(id);
            if (newDefaultWarehouse == null)
            {
                return false;
            }

            newDefaultWarehouse.IsDefault = true;
            await warehouseRepository.UpdateAsync(newDefaultWarehouse);

            return true;
        }

        public async Task<bool> HasProductsAsync(Guid id)
        {
            return await warehouseRepository.HasProductsAsync(id);
        }

        public async Task CreateWarehouseAsync(CreateWarehouseDto createDto)
        {
            // Automatically set as default only if no default warehouse exists yet
            Warehouse? existingDefault = await warehouseRepository.GetDefaultWarehouseAsync();
            bool isDefault = existingDefault is null;

            Warehouse warehouse = new()
            {
                Name = createDto.Name,
                Address = createDto.Address,
                IsDefault = isDefault
            };

            await warehouseRepository.CreateAsync(warehouse);
        }

        public async Task UpdateWarehouseAsync(Guid id, UpdateWarehouseDto updateDto)
        {
            Warehouse? warehouse = await warehouseRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Warehouse with ID {id} not found");

            warehouse.Name = updateDto.Name;
            warehouse.Address = updateDto.Address;

            await warehouseRepository.UpdateAsync(warehouse);
        }

        public async Task<bool> SetActiveStatusAsync(Guid id, bool isActive)
        {
            return await warehouseRepository.SetActiveStatusAsync(id, isActive);
        }

        private static WarehouseDto MapToDto(Warehouse warehouse, bool hasProducts = false)
        {
            return new WarehouseDto
            {
                Id = warehouse.Id,
                Name = warehouse.Name,
                Address = warehouse.Address,
                IsDefault = warehouse.IsDefault,
                IsActive = warehouse.IsActive,
                CreatedAt = warehouse.CreatedAt,
                HasProducts = hasProducts
            };
        }
    }
}