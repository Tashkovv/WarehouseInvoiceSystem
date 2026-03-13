namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.DTOs.Individual;
    using WarehouseInvoiceSystem.Application.DTOs.PurchaseNote;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public class IndividualService(IIndividualRepository individualRepository,
                                   IPurchaseNoteService purchaseNoteService) : IIndividualService
    {
        public async Task<IEnumerable<IndividualDto>> GetAllIndividualsAsync()
        {
            IEnumerable<Individual> individuals = await individualRepository.GetAllAsync();
            return individuals.Select(MapToDto);
        }

        public async Task<PagedResult<IndividualDto>> GetPagedAsync(GetIndividualsQuery query)
        {
            PagedResult<Individual> result = await individualRepository.GetPagedAsync(query);
            return new PagedResult<IndividualDto>
            {
                Items = [.. result.Items.Select(MapToDto)],
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
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

        public async Task<IndividualAnalyticsDto> GetIndividualAnalyticsAsync(Guid individualId)
        {
            var analytics = new IndividualAnalyticsDto();

            // Get all purchase notes for this individual
            IEnumerable<PurchaseNoteDto> allPurchaseNotes = await purchaseNoteService.GetPurchaseNotesByIndividualAsync(individualId);
            var purchaseNotesList = allPurchaseNotes.ToList();

            if (purchaseNotesList.Count == 0)
            {
                return analytics;
            }

            // Overall statistics — cancelled notes excluded from totals
            var cancelledNotes = purchaseNotesList.Where(pn => pn.Status == PurchaseNoteStatus.Cancelled).ToList();
            analytics.CancelledCount = cancelledNotes.Count;
            analytics.CancelledAmount = cancelledNotes.Sum(pn => pn.TotalAmount);

            var activeNotes = purchaseNotesList.Where(pn => pn.Status != PurchaseNoteStatus.Cancelled).ToList();
            analytics.TotalPurchaseNotes = activeNotes.Count;
            analytics.TotalAmount = activeNotes.Sum(pn => pn.TotalAmount);

            // Payment status
            var paidNotes = purchaseNotesList.Where(pn => pn.Status == PurchaseNoteStatus.Paid).ToList();
            analytics.PaidCount = paidNotes.Count;
            analytics.PaidAmount = paidNotes.Sum(pn => pn.TotalAmount);

            var unpaidNotes = purchaseNotesList.Where(pn =>
                pn.Status == PurchaseNoteStatus.Draft ||
                pn.Status == PurchaseNoteStatus.Pending).ToList();
            analytics.UnpaidCount = unpaidNotes.Count;
            analytics.UnpaidAmount = unpaidNotes.Sum(pn => pn.TotalAmount);

            // Date range
            analytics.FirstPurchaseDate = purchaseNotesList.Min(pn => pn.PurchaseDate);
            analytics.LastPurchaseDate = purchaseNotesList.Max(pn => pn.PurchaseDate);

            // Most purchased product
            var productStats = purchaseNotesList
                .SelectMany(pn => pn.LineItems)
                .GroupBy(li => new { li.ProductId, li.ProductName, li.ProductCode, li.ProductUnit })
                .Select(g => new
                {
                    g.Key.ProductName,
                    g.Key.ProductCode,
                    TotalQuantity = g.Sum(li => li.Quantity),
                    Unit = g.Key.ProductUnit
                })
                .OrderByDescending(p => p.TotalQuantity)
                .FirstOrDefault();

            if (productStats != null)
            {
                analytics.MostPurchasedProduct = productStats.ProductName;
                analytics.MostPurchasedProductQuantity = productStats.TotalQuantity;
                analytics.MostPurchasedProductUnit = productStats.Unit;
            }

            // Recent purchase notes
            analytics.RecentPurchaseNotes = purchaseNotesList
                .OrderByDescending(pn => pn.PurchaseDate)
                .Take(5)
                .Select(pn => new RecentPurchaseNoteDto
                {
                    Id = pn.Id,
                    NoteNumber = pn.NoteNumber,
                    PurchaseDate = pn.PurchaseDate,
                    TotalAmount = pn.TotalAmount,
                    Status = pn.Status
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