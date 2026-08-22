using System.Security.Cryptography;
using System.Text;
using CreditManagement.Application.DTOs;
using CreditManagement.Application.Exceptions;
using CreditManagement.Application.Interfaces;
using CreditManagement.Domain.Entities;
using CreditManagement.Domain.Interfaces;

namespace CreditManagement.Application.Services;

// Orchestrates Card business logic, validation, and security rules
public class CardService : ICardService
{
    private readonly ICardRepository _cardRepository;
    private readonly IBillRepository _billRepository;

    public CardService(ICardRepository cardRepository, IBillRepository billRepository)
    {
        _cardRepository = cardRepository;
        _billRepository = billRepository;
    }

    // ── 1. ADD NEW CARD ──
    public async Task<CardResponseDto> AddCardAsync(Guid userId, AddCardRequestDto request)
    {
        // Validate card number using Luhn checksum algorithm (Mod 10)
        if (!IsValidLuhn(request.CardNumber))
        {
            throw new CustomValidationException("Invalid credit card number checksum (failed Luhn algorithm check).");
        }

        // Check for duplicate card using SHA-256 hash (prevents storing raw numbers)
        var cardHash = ComputeCardHash(request.CardNumber);
        var existingCard = await _cardRepository.GetByCardHashAsync(cardHash);
        if (existingCard is not null)
        {
            throw new ConflictException("This credit card is already registered in the system.");
        }

        var card = new Card
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CardHolderName = request.CardHolderName.Trim(),
            CardNumberMasked = MaskCardNumber(request.CardNumber),
            CardNumberHash = cardHash,
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            Issuer = DetectIssuer(request.CardNumber),
            CreditLimit = request.CreditLimit,
            OutstandingAmount = 0,
            CreatedAt = DateTime.UtcNow
        };

        await _cardRepository.AddAsync(card);

        return MapToResponseDto(card);
    }

    // ── 2. GET ALL CARDS FOR LOGGED-IN USER ──
    public async Task<IEnumerable<CardResponseDto>> GetCardsByUserIdAsync(Guid userId)
    {
        var cards = await _cardRepository.GetByUserIdAsync(userId);
        return cards.Select(MapToResponseDto);
    }

    // ── 3. GET CARD DETAILS BY ID (WITH RECENT BILLS) ──
    public async Task<CardDetailsDto> GetCardDetailsAsync(Guid userId, Guid cardId)
    {
        var card = await _cardRepository.GetByIdAsync(cardId)
            ?? throw new NotFoundException($"Card with ID '{cardId}' not found.");

        // Security check: verify the authenticated user owns this card
        if (card.UserId != userId)
        {
            throw new UnauthorizedException("You do not have permission to view this card.");
        }

        var bills = await _billRepository.GetByCardIdAsync(cardId);

        return new CardDetailsDto
        {
            Id = card.Id,
            CardHolderName = card.CardHolderName,
            CardNumberMasked = card.CardNumberMasked,
            ExpiryMonth = card.ExpiryMonth,
            ExpiryYear = card.ExpiryYear,
            Issuer = card.Issuer,
            CreditLimit = card.CreditLimit,
            OutstandingAmount = card.OutstandingAmount,
            CreatedAt = card.CreatedAt,
            RecentBills = bills.Select(b => new BillResponseDto
            {
                Id = b.Id,
                CardId = b.CardId,
                BillingCycleStart = b.BillingCycleStart,
                BillingCycleEnd = b.BillingCycleEnd,
                TotalAmount = b.TotalAmount,
                MinimumDue = b.MinimumDue,
                DueDate = b.DueDate,
                Status = b.Status,
                GeneratedAt = b.GeneratedAt
            })
        };
    }

    // ── 4. UPDATE CARD DETAILS ──
    public async Task<CardResponseDto> UpdateCardAsync(Guid userId, Guid cardId, UpdateCardRequestDto request)
    {
        var card = await _cardRepository.GetByIdAsync(cardId)
            ?? throw new NotFoundException($"Card with ID '{cardId}' not found.");

        if (card.UserId != userId)
        {
            throw new UnauthorizedException("You do not have permission to modify this card.");
        }

        card.CardHolderName = request.CardHolderName.Trim();
        card.CreditLimit = request.CreditLimit;

        await _cardRepository.UpdateAsync(card);

        return MapToResponseDto(card);
    }

    // ── 5. DELETE CARD ──
    public async Task DeleteCardAsync(Guid userId, Guid cardId)
    {
        var card = await _cardRepository.GetByIdAsync(cardId)
            ?? throw new NotFoundException($"Card with ID '{cardId}' not found.");

        if (card.UserId != userId)
        {
            throw new UnauthorizedException("You do not have permission to delete this card.");
        }

        await _cardRepository.DeleteAsync(card);
    }

    // ── HELPER: LUHN CHECKSUM VALIDATION (MOD 10) ──
    private static bool IsValidLuhn(string cardNumber)
    {
        var digitsOnly = new string(cardNumber.Where(char.IsDigit).ToArray());
        if (digitsOnly.Length < 13 || digitsOnly.Length > 19) return false;

        int sum = 0;
        bool alternate = false;

        for (int i = digitsOnly.Length - 1; i >= 0; i--)
        {
            int digit = digitsOnly[i] - '0';

            if (alternate)
            {
                digit *= 2;
                if (digit > 9) digit -= 9;
            }

            sum += digit;
            alternate = !alternate;
        }

        return sum % 10 == 0;
    }

    // ── HELPER: CARD ISSUER DETECTION BY BIN PREFIX ──
    private static string DetectIssuer(string cardNumber)
    {
        var digits = new string(cardNumber.Where(char.IsDigit).ToArray());

        if (digits.StartsWith("4")) return "Visa";
        if (digits.StartsWith("34") || digits.StartsWith("37")) return "Amex";

        if (digits.StartsWith("51") || digits.StartsWith("52") || digits.StartsWith("53") ||
            digits.StartsWith("54") || digits.StartsWith("55") ||
            (digits.Length >= 4 && int.TryParse(digits[..4], out int mc) && mc >= 2221 && mc <= 2720))
        {
            return "Mastercard";
        }

        if (digits.StartsWith("60") || digits.StartsWith("65") || digits.StartsWith("81") ||
            digits.StartsWith("82") || digits.StartsWith("508"))
        {
            return "RuPay";
        }

        return "Unknown";
    }

    // ── HELPER: MASK SENSITIVE CARD NUMBER ──
    private static string MaskCardNumber(string cardNumber)
    {
        var digits = new string(cardNumber.Where(char.IsDigit).ToArray());
        if (digits.Length < 4) return "****";
        var last4 = digits[^4..];
        return $"**** **** **** {last4}";
    }

    // ── HELPER: COMPUTE SHA-256 HASH (FOR DUPLICATE DETECTION) ──
    private static string ComputeCardHash(string cardNumber)
    {
        var digits = new string(cardNumber.Where(char.IsDigit).ToArray());
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(digits));
        return Convert.ToHexString(bytes);
    }

    // ── HELPER: DTO MAPPING ──
    private static CardResponseDto MapToResponseDto(Card card) => new()
    {
        Id = card.Id,
        CardHolderName = card.CardHolderName,
        CardNumberMasked = card.CardNumberMasked,
        ExpiryMonth = card.ExpiryMonth,
        ExpiryYear = card.ExpiryYear,
        Issuer = card.Issuer,
        CreditLimit = card.CreditLimit,
        OutstandingAmount = card.OutstandingAmount,
        CreatedAt = card.CreatedAt
    };
}
