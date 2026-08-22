using CreditManagement.Domain.Entities;

namespace CreditManagement.Domain.Interfaces;

// Repository contract for Card data access
public interface ICardRepository
{
    Task<Card?> GetByIdAsync(Guid id);
    Task<IEnumerable<Card>> GetByUserIdAsync(Guid userId);
    Task<Card?> GetByCardHashAsync(string cardHash);
    Task AddAsync(Card card);
    Task UpdateAsync(Card card);
    Task DeleteAsync(Card card);
}
