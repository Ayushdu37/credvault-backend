using CreditManagement.Application.DTOs;
using CreditManagement.Application.Exceptions;
using CreditManagement.Application.Services;
using CreditManagement.Domain.Entities;
using CreditManagement.Domain.Interfaces;
using Moq;

namespace CreditManagement.Tests;

public class CardServiceTests
{
    private readonly Mock<ICardRepository> _cardRepo = new();
    private readonly Mock<IBillRepository> _billRepo = new();
    private readonly CardService _cardService;
    private readonly Guid _userId = Guid.NewGuid();

    public CardServiceTests()
    {
        _cardService = new CardService(_cardRepo.Object, _billRepo.Object);
    }

    // ── PASS TESTS ──

    [Fact]
    public async Task AddCard_WithValidVisa_ReturnsCardWithIssuerVisa()
    {
        var request = new AddCardRequestDto
        {
            CardNumber = "4111111111111111", CardHolderName = "Test",
            ExpiryMonth = 12, ExpiryYear = 2028, CreditLimit = 100000
        };
        _cardRepo.Setup(r => r.GetByCardHashAsync(It.IsAny<string>())).ReturnsAsync((Card?)null);

        var result = await _cardService.AddCardAsync(_userId, request);

        Assert.Equal("Visa", result.Issuer);
        Assert.Equal("**** **** **** 1111", result.CardNumberMasked);
    }

    [Fact]
    public async Task AddCard_WithMastercard_DetectsCorrectIssuer()
    {
        var request = new AddCardRequestDto
        {
            CardNumber = "5500000000000004", CardHolderName = "Test",
            ExpiryMonth = 6, ExpiryYear = 2027, CreditLimit = 50000
        };
        _cardRepo.Setup(r => r.GetByCardHashAsync(It.IsAny<string>())).ReturnsAsync((Card?)null);

        var result = await _cardService.AddCardAsync(_userId, request);

        Assert.Equal("Mastercard", result.Issuer);
    }

    [Fact]
    public async Task AddCard_MasksCardNumber_ShowsOnlyLast4()
    {
        var request = new AddCardRequestDto
        {
            CardNumber = "4111111111111111", CardHolderName = "Test",
            ExpiryMonth = 12, ExpiryYear = 2028, CreditLimit = 100000
        };
        _cardRepo.Setup(r => r.GetByCardHashAsync(It.IsAny<string>())).ReturnsAsync((Card?)null);

        var result = await _cardService.AddCardAsync(_userId, request);

        Assert.DoesNotContain("4111111111111111", result.CardNumberMasked);
        Assert.EndsWith("1111", result.CardNumberMasked);
    }

    [Fact]
    public async Task GetCards_ReturnsAllUserCards()
    {
        var cards = new List<Card>
        {
            new() { Id = Guid.NewGuid(), UserId = _userId, CardHolderName = "Card1", CardNumberMasked = "****1111", Issuer = "Visa" },
            new() { Id = Guid.NewGuid(), UserId = _userId, CardHolderName = "Card2", CardNumberMasked = "****2222", Issuer = "Mastercard" }
        };
        _cardRepo.Setup(r => r.GetByUserIdAsync(_userId)).ReturnsAsync(cards);

        var result = await _cardService.GetCardsByUserIdAsync(_userId);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task DeleteCard_WithOwnedCard_CallsDeleteAsync()
    {
        var cardId = Guid.NewGuid();
        _cardRepo.Setup(r => r.GetByIdAsync(cardId)).ReturnsAsync(new Card { Id = cardId, UserId = _userId });

        await _cardService.DeleteCardAsync(_userId, cardId);

        _cardRepo.Verify(r => r.DeleteAsync(It.IsAny<Card>()), Times.Once);
    }

    // ── FAIL TESTS ──

    [Fact]
    public async Task AddCard_WithInvalidLuhn_ThrowsValidation()
    {
        var request = new AddCardRequestDto
        {
            CardNumber = "1234567890123456", CardHolderName = "Test",
            ExpiryMonth = 12, ExpiryYear = 2028, CreditLimit = 100000
        };

        await Assert.ThrowsAsync<CustomValidationException>(() => _cardService.AddCardAsync(_userId, request));
    }

    [Fact]
    public async Task AddCard_WithDuplicateCard_ThrowsConflict()
    {
        var request = new AddCardRequestDto
        {
            CardNumber = "4111111111111111", CardHolderName = "Test",
            ExpiryMonth = 12, ExpiryYear = 2028, CreditLimit = 100000
        };
        _cardRepo.Setup(r => r.GetByCardHashAsync(It.IsAny<string>()))
            .ReturnsAsync(new Card { CardNumberHash = "existing" });

        await Assert.ThrowsAsync<ConflictException>(() => _cardService.AddCardAsync(_userId, request));
    }

    [Fact]
    public async Task GetCardDetails_WithNonExistentCard_ThrowsNotFound()
    {
        _cardRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Card?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _cardService.GetCardDetailsAsync(_userId, Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateCard_OwnedByAnotherUser_ThrowsUnauthorized()
    {
        var cardId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        _cardRepo.Setup(r => r.GetByIdAsync(cardId)).ReturnsAsync(new Card { Id = cardId, UserId = otherUserId });

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _cardService.UpdateCardAsync(_userId, cardId, new UpdateCardRequestDto { CardHolderName = "Hack", CreditLimit = 999999 }));
    }

    [Fact]
    public async Task DeleteCard_OwnedByAnotherUser_ThrowsUnauthorized()
    {
        var cardId = Guid.NewGuid();
        _cardRepo.Setup(r => r.GetByIdAsync(cardId)).ReturnsAsync(new Card { Id = cardId, UserId = Guid.NewGuid() });

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _cardService.DeleteCardAsync(_userId, cardId));
    }
}
