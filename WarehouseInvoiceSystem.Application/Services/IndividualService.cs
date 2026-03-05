namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.DTOs.Individual;
    using WarehouseInvoiceSystem.Application.DTOs.PurchaseNote;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;

    public class IndividualService(IIndividualRepository individualRepository,
                                   IPurchaseNoteService purchaseNoteService) : IIndividualService
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

        public async Task<IndividualAnalyticsDto> GetIndividualAnalyticsAsync(Guid individualId)
        {
            Individual? individual = await individualRepository.GetByIdAsync(individualId)
                ?? throw new KeyNotFoundException($"Individual with ID {individualId} not found");

            var analytics = new IndividualAnalyticsDto();

            // Get all purchase notes for this individual
            IEnumerable<PurchaseNoteDto> allPurchaseNotes = await purchaseNoteService.GetPurchaseNotesByIndividualAsync(individualId);
            var purchaseNotesList = allPurchaseNotes.ToList();

            if (!purchaseNotesList.Any())
            {
                return analytics; // Return empty analytics
            }

            // Overall statistics
            analytics.TotalPurchaseNotes = purchaseNotesList.Count;
            analytics.TotalAmount = purchaseNotesList.Sum(pn => pn.TotalAmount);

            // Payment status
            var paidNotes = purchaseNotesList.Where(pn => pn.Status == Domain.Enums.PurchaseNoteStatus.Paid).ToList();
            analytics.PaidCount = paidNotes.Count;
            analytics.PaidAmount = paidNotes.Sum(pn => pn.TotalAmount);

            var unpaidNotes = purchaseNotesList.Where(pn =>
                pn.Status == Domain.Enums.PurchaseNoteStatus.Draft ||
                pn.Status == Domain.Enums.PurchaseNoteStatus.Completed).ToList();
            analytics.UnpaidCount = unpaidNotes.Count;
            analytics.UnpaidAmount = unpaidNotes.Sum(pn => pn.TotalAmount);

            // Date range
            analytics.FirstPurchaseDate = purchaseNotesList.Min(pn => pn.PurchaseDate);
            analytics.LastPurchaseDate = purchaseNotesList.Max(pn => pn.PurchaseDate);

            // Most purchased product
            var productStats = purchaseNotesList
                .SelectMany(pn => pn.LineItems)
                .GroupBy(li => new { li.ProductId, li.ProductName, li.ProductCode })
                .Select(g => new
                {
                    g.Key.ProductName,
                    g.Key.ProductCode,
                    TotalQuantity = g.Sum(li => li.Quantity),
                    // Extract unit from first line item (assuming consistent)
                    Unit = g.First().Description?.Split(' ').LastOrDefault() ?? ""
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
                .Take(10)
                .Select(pn => new RecentPurchaseNoteDto
                {
                    Id = pn.Id,
                    NoteNumber = pn.NoteNumber,
                    PurchaseDate = pn.PurchaseDate,
                    TotalAmount = pn.TotalAmount,
                    Status = pn.Status.ToString()
                })
                .ToList();

            return analytics;
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