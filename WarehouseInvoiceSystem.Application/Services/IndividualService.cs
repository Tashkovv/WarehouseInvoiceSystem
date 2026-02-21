namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.DTOs.Individual;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;

    public class IndividualService(IIndividualRepository individualRepository) : IIndividualService
    {
        public async Task<IEnumerable<IndividualDto>> GetAllIndividualsAsync()
        {
            IEnumerable<Individual> individuals = await individualRepository.GetAllAsync();
            return individuals.Select(MapToDto);
        }

        public async Task<IEnumerable<IndividualDto>> GetActiveIndividualsAsync()
        {
            IEnumerable<Individual> individuals = await individualRepository.GetActiveIndividualsAsync();
            return individuals.Select(MapToDto);
        }

        public async Task<IndividualDto?> GetIndividualByIdAsync(Guid id)
        {
            Individual? individual = await individualRepository.GetByIdAsync(id);
            return individual == null ? null : MapToDto(individual);
        }

        public async Task<IndividualDto?> GetIndividualByIdentificationNumberAsync(string identificationNumber)
        {
            Individual? individual = await individualRepository.GetByIdentificationNumberAsync(identificationNumber);
            return individual == null ? null : MapToDto(individual);
        }

        public async Task<IndividualDto> CreateIndividualAsync(CreateIndividualDto createDto)
        {
            // Validate unique identification number
            if (await individualRepository.IdentificationNumberExistsAsync(createDto.IdentificationNumber))
                throw new InvalidOperationException($"Individual with identification number '{createDto.IdentificationNumber}' already exists");

            Individual individual = new()
            {
                FirstName = createDto.FirstName,
                LastName = createDto.LastName,
                IdentificationNumber = createDto.IdentificationNumber,
                Address = createDto.Address,
                Phone = createDto.Phone,
                Email = createDto.Email,
                IsActive = createDto.IsActive
            };

            Individual created = await individualRepository.CreateAsync(individual);
            return MapToDto(created);
        }

        public async Task<IndividualDto> UpdateIndividualAsync(Guid id, UpdateIndividualDto updateDto)
        {
            Individual? individual = await individualRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Individual with ID {id} not found");

            // Validate unique identification number
            if (await individualRepository.IdentificationNumberExistsAsync(updateDto.IdentificationNumber, id))
                throw new InvalidOperationException($"Individual with identification number '{updateDto.IdentificationNumber}' already exists");

            individual.FirstName = updateDto.FirstName;
            individual.LastName = updateDto.LastName;
            individual.IdentificationNumber = updateDto.IdentificationNumber;
            individual.Address = updateDto.Address;
            individual.Phone = updateDto.Phone;
            individual.Email = updateDto.Email;
            individual.IsActive = updateDto.IsActive;

            Individual updated = await individualRepository.UpdateAsync(individual);
            return MapToDto(updated);
        }

        public async Task<bool> DeleteIndividualAsync(Guid id)
        {
            return await individualRepository.DeleteAsync(id);
        }

        private static IndividualDto MapToDto(Individual individual)
        {
            return new IndividualDto
            {
                Id = individual.Id,
                FirstName = individual.FirstName,
                LastName = individual.LastName,
                FullName = individual.FullName,
                IdentificationNumber = individual.IdentificationNumber,
                Address = individual.Address,
                Phone = individual.Phone,
                Email = individual.Email,
                IsActive = individual.IsActive,
                CreatedAt = individual.CreatedAt
            };
        }
    }
}