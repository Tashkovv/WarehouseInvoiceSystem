namespace WarehouseInvoiceSystem.Tests.Services.PurchaseNoteService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Application.DTOs.PurchaseNote;
using WarehouseInvoiceSystem.Domain.Entities;

/// <summary>
/// Tests for CalculateNetQuantity (private static) via CreatePurchaseNoteAsync.
/// Kalo reduces gross quantity: net = Round(gross * (1 - kalo/100), AwayFromZero).
/// </summary>
public class CalculateKaloTests : PurchaseNoteServiceTestBase
{
    [Fact]
    public async Task ZeroKalo_NetEqualsGross()
    {
        var capturedNote = await CreateWithKalo(grossQuantity: 100m, kaloPercentage: 0m);

        capturedNote.LineItems.First().Quantity.Should().Be(100m);
    }

    [Fact]
    public async Task TenPercentKalo_ReducesQuantity()
    {
        var capturedNote = await CreateWithKalo(grossQuantity: 100m, kaloPercentage: 10m);

        capturedNote.LineItems.First().Quantity.Should().Be(90m);
    }

    [Fact]
    public async Task HundredPercentKalo_ZeroNet()
    {
        var capturedNote = await CreateWithKalo(grossQuantity: 100m, kaloPercentage: 100m);

        capturedNote.LineItems.First().Quantity.Should().Be(0m);
    }

    [Fact]
    public async Task Rounding_AwayFromZero()
    {
        // 3 * (1 - 33.333.../100) = 3 * 0.66666... = 2.0000... → rounds to 2
        // Without AwayFromZero, banker's rounding of 2.5 would give 2 (round to even).
        // Test: gross=5, kalo=10% → 5*0.9=4.5 → AwayFromZero rounds to 5
        var capturedNote = await CreateWithKalo(grossQuantity: 5m, kaloPercentage: 10m);

        capturedNote.LineItems.First().Quantity.Should().Be(5m);
    }

    [Fact]
    public async Task NoFractionalResult_NoDecimalPlaces()
    {
        // gross=10, kalo=5% → 10 * 0.95 = 9.5 → AwayFromZero → 10
        var capturedNote = await CreateWithKalo(grossQuantity: 10m, kaloPercentage: 5m);

        capturedNote.LineItems.First().Quantity.Should().Be(10m);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private async Task<PurchaseNote> CreateWithKalo(decimal grossQuantity, decimal kaloPercentage)
    {
        PurchaseNote? capturedNote = null;

        IndividualRepo.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        WarehouseRepo.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        ProductRepo.AllExistAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(true);
        PurchaseNoteRepo.GenerateNoteNumberAsync(Arg.Any<CancellationToken>()).Returns("OB-000001");
        PurchaseNoteRepo.When(r => r.CreateAsync(Arg.Any<PurchaseNote>()))
                        .Do(ci => capturedNote = ci.Arg<PurchaseNote>());

        var dto = new CreatePurchaseNoteDto
        {
            IndividualId = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            PurchaseDate = DateTime.Today,
            LineItems =
            [
                new CreatePurchaseNoteLineDto
                {
                    ProductId = Guid.NewGuid(),
                    Description = "Test",
                    GrossQuantity = grossQuantity,
                    KaloPercentage = kaloPercentage,
                    Quantity = 0m,   // intentionally wrong — service recalculates
                    UnitPrice = 10m
                }
            ]
        };

        await CreateService().CreatePurchaseNoteAsync(dto);

        capturedNote.Should().NotBeNull("CreateAsync should have been called");
        return capturedNote!;
    }
}
