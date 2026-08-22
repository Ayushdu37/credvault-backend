using CreditManagement.Application.DTOs;

namespace CreditManagement.Application.Interfaces;

// Service interface orchestrating Card management use cases
public interface ICardService
{
    Task<CardResponseDto> AddCardAsync(Guid userId, AddCardRequestDto request);
    Task<IEnumerable<CardResponseDto>> GetCardsByUserIdAsync(Guid userId);
    Task<CardDetailsDto> GetCardDetailsAsync(Guid userId, Guid cardId);
    Task<CardResponseDto> UpdateCardAsync(Guid userId, Guid cardId, UpdateCardRequestDto request);
    Task DeleteCardAsync(Guid userId, Guid cardId);
}
