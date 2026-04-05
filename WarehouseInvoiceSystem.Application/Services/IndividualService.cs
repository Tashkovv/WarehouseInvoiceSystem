namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.DTOs.Individual;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public class IndividualService(IIndividualRepository individualRepository,
                                   IPurchaseNoteRepository purchaseNoteRepository) : IIndividualService
    {
        public async Task<IEnumerable<IndividualDto>> GetAllIndividualsAsync(CancellationToken ct = default)
        {
            IEnumerable<Individual> individuals = await individualRepository.GetAllAsync(ct);
            return individuals.Select(MapToDto);
        }

        public async Task<PagedResult<IndividualDto>> GetPagedAsync(GetIndividualsQuery query, CancellationToken ct = default)
        {
            PagedResult<Individual> result = await individualRepository.GetPagedAsync(query, ct);
            return new PagedResult<IndividualDto>
            {
                Items = [.. result.Items.Select(MapToDto)],
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<IEnumerable<IndividualDto>> GetActiveIndividualsAsync(CancellationToken ct = default)
        {
            IEnumerable<Individual> individuals = await individualRepository.GetActiveIndividualsAsync(ct);
            return individuals.Select(MapToDto);
        }

        public async Task<IndividualDto?> GetIndividualByIdAsync(Guid id, CancellationToken ct = default)
        {
            Individual? individual = await individualRepository.GetByIdAsync(id, ct);
            return individual == null ? null : MapToDto(individual);
        }

        public async Task<IndividualDto?> GetIndividualByIdentificationNumberAsync(string identificationNumber, CancellationToken ct = default)
        {
            Individual? individual = await individualRepository.GetByIdentificationNumberAsync(identificationNumber, ct);
            return individual == null ? null : MapToDto(individual);
        }

        public async Task<IndividualAnalyticsDto> GetIndividualAnalyticsAsync(Guid individualId, CancellationToken ct = default)
        {
            var data = await purchaseNoteRepository.GetIndividualAnalyticsDataAsync(individualId, ct);

            if (data.StatRows.Count == 0 && data.RecentNotes.Count == 0)
                return new IndividualAnalyticsDto();

            var analytics = new IndividualAnalyticsDto();
            var rows = data.StatRows;

            var cancelled = rows.Where(r => r.Status == PurchaseNoteStatus.Cancelled).ToList();
            analytics.CancelledCount  = cancelled.Sum(r => r.Count);
            analytics.CancelledAmount = cancelled.Sum(r => r.TotalAmount);

            var active = rows.Where(r => r.Status != PurchaseNoteStatus.Cancelled && r.Status != PurchaseNoteStatus.Draft).ToList();
            analytics.TotalPurchaseNotes = active.Sum(r => r.Count);
            analytics.TotalAmount        = active.Sum(r => r.TotalAmount);

            var paid = rows.Where(r => r.Status == PurchaseNoteStatus.Paid).ToList();
            analytics.PaidCount  = paid.Sum(r => r.Count);
            analytics.PaidAmount = paid.Sum(r => r.TotalAmount);

            var unpaid = rows.Where(r => r.Status == PurchaseNoteStatus.Pending).ToList();
            analytics.UnpaidCount  = unpaid.Sum(r => r.Count);
            analytics.UnpaidAmount = unpaid.Sum(r => r.TotalAmount);

            analytics.FirstPurchaseDate = data.FirstPurchaseDate;
            analytics.LastPurchaseDate  = data.LastPurchaseDate;

            if (data.MostPurchasedProductName is not null)
            {
                analytics.MostPurchasedProduct         = data.MostPurchasedProductName;
                analytics.MostPurchasedProductQuantity = data.MostPurchasedProductQuantity;
                analytics.MostPurchasedProductUnit     = data.MostPurchasedProductUnit;
            }

            analytics.RecentPurchaseNotes = data.RecentNotes
                .Select(r => new RecentPurchaseNoteDto
                {
                    Id           = r.Id,
                    NoteNumber   = r.NoteNumber,
                    PurchaseDate = r.PurchaseDate,
                    TotalAmount  = r.TotalAmount,
                    Status       = r.Status
                })
                .ToList();

            return analytics;
        }

        public async Task CreateIndividualAsync(CreateIndividualDto createDto)
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
                BankAccount = createDto.BankAccount,
                IsActive = createDto.IsActive
            };

            await individualRepository.CreateAsync(individual);
        }

        public async Task UpdateIndividualAsync(Guid id, UpdateIndividualDto updateDto)
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
            individual.BankAccount = updateDto.BankAccount;
            individual.IsActive = updateDto.IsActive;

            await individualRepository.UpdateAsync(individual);
        }

        public async Task<bool> DeleteIndividualAsync(Guid id)
        {
            return await individualRepository.DeleteAsync(id);
        }

        public async Task<bool> SetActiveStatusAsync(Guid id, bool isActive)
        {
            return await individualRepository.SetActiveStatusAsync(id, isActive);
        }

        public Task<(int total, int active)> GetCountsAsync(CancellationToken ct = default)
            => individualRepository.GetCountsAsync(ct);

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
                BankAccount = individual.BankAccount,
                IsActive = individual.IsActive,
                CreatedAt = individual.CreatedAt
            };
        }
    }
}