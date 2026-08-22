using CreditManagement.Domain.Entities;

namespace CreditManagement.Domain.Interfaces;

// Repository contract for Payment data access
public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id);
    Task<IEnumerable<Payment>> GetByUserIdAsync(Guid userId);
    Task<Payment?> GetByTransactionReferenceAsync(string transactionReference);
    Task AddAsync(Payment payment);
}
