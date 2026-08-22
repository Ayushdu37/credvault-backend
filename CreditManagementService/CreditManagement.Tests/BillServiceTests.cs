using CreditManagement.Application.DTOs;
using CreditManagement.Application.Exceptions;
using CreditManagement.Application.Services;
using CreditManagement.Domain.Entities;
using CreditManagement.Domain.Enums;
using CreditManagement.Domain.Interfaces;
using Moq;

namespace CreditManagement.Tests;

public class BillServiceTests
{
    private readonly Mock<IBillRepository> _billRepo = new();
    private readonly Mock<ICardRepository> _cardRepo = new();
    private readonly Mock<IPaymentRepository> _paymentRepo = new();
    private readonly BillService _billService;
    private readonly Guid _userId = Guid.NewGuid();

    public BillServiceTests()
    {
        _billService = new BillService(_billRepo.Object, _cardRepo.Object, _paymentRepo.Object);
    }

    // ── PASS TESTS ──

    [Fact]
    public async Task GenerateBill_CalculatesMinDue_As5Percent()
    {
        var cardId = Guid.NewGuid();
        _cardRepo.Setup(r => r.GetByIdAsync(cardId)).ReturnsAsync(new Card { Id = cardId, UserId = _userId });

        var request = new GenerateBillRequestDto
        {
            BillingCycleStart = new DateOnly(2026, 7, 1),
            BillingCycleEnd = new DateOnly(2026, 7, 31),
            TotalAmount = 10000
        };

        var result = await _billService.GenerateBillAsync(_userId, cardId, request);

        Assert.Equal(500.00m, result.MinimumDue); // 5% of 10000
    }

    [Fact]
    public async Task GenerateBill_CalculatesDueDate_Plus18Days()
    {
        var cardId = Guid.NewGuid();
        _cardRepo.Setup(r => r.GetByIdAsync(cardId)).ReturnsAsync(new Card { Id = cardId, UserId = _userId });

        var request = new GenerateBillRequestDto
        {
            BillingCycleStart = new DateOnly(2026, 7, 1),
            BillingCycleEnd = new DateOnly(2026, 7, 31),
            TotalAmount = 5000
        };

        var result = await _billService.GenerateBillAsync(_userId, cardId, request);

        Assert.Equal(new DateOnly(2026, 8, 18), result.DueDate); // July 31 + 18
    }

    [Fact]
    public async Task GenerateBill_WithCustomMinDue_UsesProvidedValue()
    {
        var cardId = Guid.NewGuid();
        _cardRepo.Setup(r => r.GetByIdAsync(cardId)).ReturnsAsync(new Card { Id = cardId, UserId = _userId });

        var request = new GenerateBillRequestDto
        {
            BillingCycleStart = new DateOnly(2026, 7, 1),
            BillingCycleEnd = new DateOnly(2026, 7, 31),
            TotalAmount = 10000,
            MinimumDue = 2000
        };

        var result = await _billService.GenerateBillAsync(_userId, cardId, request);

        Assert.Equal(2000m, result.MinimumDue);
    }

    [Fact]
    public async Task GenerateBill_WithZeroAmount_SetsBillStatusToPaid()
    {
        var cardId = Guid.NewGuid();
        _cardRepo.Setup(r => r.GetByIdAsync(cardId)).ReturnsAsync(new Card { Id = cardId, UserId = _userId });

        var request = new GenerateBillRequestDto
        {
            BillingCycleStart = new DateOnly(2026, 7, 1),
            BillingCycleEnd = new DateOnly(2026, 7, 31),
            TotalAmount = 0
        };

        var result = await _billService.GenerateBillAsync(_userId, cardId, request);

        Assert.Equal(BillStatus.Paid, result.Status);
    }

    [Fact]
    public async Task GenerateBill_UpdatesCardOutstandingAmount()
    {
        var cardId = Guid.NewGuid();
        var card = new Card { Id = cardId, UserId = _userId, OutstandingAmount = 5000 };
        _cardRepo.Setup(r => r.GetByIdAsync(cardId)).ReturnsAsync(card);

        var request = new GenerateBillRequestDto
        {
            BillingCycleStart = new DateOnly(2026, 7, 1),
            BillingCycleEnd = new DateOnly(2026, 7, 31),
            TotalAmount = 3000
        };

        await _billService.GenerateBillAsync(_userId, cardId, request);

        Assert.Equal(8000m, card.OutstandingAmount); // 5000 + 3000
    }

    // ── FAIL TESTS ──

    [Fact]
    public async Task GenerateBill_WithNonExistentCard_ThrowsNotFound()
    {
        _cardRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Card?)null);

        var request = new GenerateBillRequestDto
        {
            BillingCycleStart = new DateOnly(2026, 7, 1),
            BillingCycleEnd = new DateOnly(2026, 7, 31),
            TotalAmount = 1000
        };

        await Assert.ThrowsAsync<NotFoundException>(
            () => _billService.GenerateBillAsync(_userId, Guid.NewGuid(), request));
    }

    [Fact]
    public async Task GenerateBill_ForOtherUsersCard_ThrowsUnauthorized()
    {
        var cardId = Guid.NewGuid();
        _cardRepo.Setup(r => r.GetByIdAsync(cardId)).ReturnsAsync(new Card { Id = cardId, UserId = Guid.NewGuid() });

        var request = new GenerateBillRequestDto
        {
            BillingCycleStart = new DateOnly(2026, 7, 1),
            BillingCycleEnd = new DateOnly(2026, 7, 31),
            TotalAmount = 1000
        };

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _billService.GenerateBillAsync(_userId, cardId, request));
    }

    [Fact]
    public async Task GenerateBill_WithInvalidCycleDates_ThrowsValidation()
    {
        var cardId = Guid.NewGuid();
        _cardRepo.Setup(r => r.GetByIdAsync(cardId)).ReturnsAsync(new Card { Id = cardId, UserId = _userId });

        var request = new GenerateBillRequestDto
        {
            BillingCycleStart = new DateOnly(2026, 7, 31), // Start AFTER end
            BillingCycleEnd = new DateOnly(2026, 7, 1),
            TotalAmount = 1000
        };

        await Assert.ThrowsAsync<CustomValidationException>(
            () => _billService.GenerateBillAsync(_userId, cardId, request));
    }

    [Fact]
    public async Task GenerateBill_WithMinDueGreaterThanTotal_ThrowsValidation()
    {
        var cardId = Guid.NewGuid();
        _cardRepo.Setup(r => r.GetByIdAsync(cardId)).ReturnsAsync(new Card { Id = cardId, UserId = _userId });

        var request = new GenerateBillRequestDto
        {
            BillingCycleStart = new DateOnly(2026, 7, 1),
            BillingCycleEnd = new DateOnly(2026, 7, 31),
            TotalAmount = 1000,
            MinimumDue = 5000 // Greater than total
        };

        await Assert.ThrowsAsync<CustomValidationException>(
            () => _billService.GenerateBillAsync(_userId, cardId, request));
    }

    [Fact]
    public async Task GetBillDetails_WithNonExistentBill_ThrowsNotFound()
    {
        _billRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Bill?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _billService.GetBillDetailsAsync(_userId, Guid.NewGuid()));
    }
}
