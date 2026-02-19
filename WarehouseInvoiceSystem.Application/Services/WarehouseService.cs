namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.DTOs.Warehouse;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;

    public class WarehouseService(IWarehouseRepository warehouseRepository) : IWarehouseService
    {
        public async Task<IEnumerable<WarehouseDto>> GetAllWarehousesAsync()
        {
            IEnumerable<Warehouse> warehouses = await warehouseRepository.GetAllAsync();
            return warehouses.Select(MapToDto);
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

        public async Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseDto createDto)
        {
            Warehouse warehouse = new()
            {
                Name = createDto.Name,
                Address = createDto.Address,
                IsDefault = createDto.IsDefault
            };

            Warehouse created = await warehouseRepository.CreateAsync(warehouse);
            return MapToDto(created);
        }

        public async Task<WarehouseDto> UpdateWarehouseAsync(Guid id, UpdateWarehouseDto updateDto)
        {
            Warehouse? warehouse = await warehouseRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Warehouse with ID {id} not found");

            warehouse.Name = updateDto.Name;
            warehouse.Address = updateDto.Address;
            warehouse.IsDefault = updateDto.IsDefault;

            Warehouse updated = await warehouseRepository.UpdateAsync(warehouse);
            return MapToDto(updated);
        }

        public async Task<bool> DeleteWarehouseAsync(Guid id)
        {
            return await warehouseRepository.DeleteAsync(id);
        }

        private static WarehouseDto MapToDto(Warehouse warehouse)
        {
            return new WarehouseDto
            {
                Id = warehouse.Id,
                Name = warehouse.Name,
                Address = warehouse.Address,
                IsDefault = warehouse.IsDefault,
                CreatedAt = warehouse.CreatedAt
            };
        }
    }
}