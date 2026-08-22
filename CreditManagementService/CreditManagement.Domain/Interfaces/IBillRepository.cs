using CreditManagement.Domain.Entities;

namespace CreditManagement.Domain.Interfaces;

// Repository contract for Bill data access
public interface IBillRepository
{
    Task<Bill?> GetByIdAsync(Guid id);
    Task<IEnumerable<Bill>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<Bill>> GetByCardIdAsync(Guid cardId);
    Task AddAsync(Bill bill);
    Task UpdateAsync(Bill bill);
}
