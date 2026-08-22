using CreditManagement.Domain.Entities;
using CreditManagement.Domain.Interfaces;
using CreditManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CreditManagement.Infrastructure.Repositories;

// Implements IPaymentRepository using EF Core
public class PaymentRepository : IPaymentRepository
{
    private readonly CreditManagementDbContext _context;

    public PaymentRepository(CreditManagementDbContext context)
    {
        _context = context;
    }

    // Include Bill -> Card chain for PaymentDetailsDto navigation
    public async Task<Payment?> GetByIdAsync(Guid id)
    {
        return await _context.Payments
            .Include(p => p.Bill)
                .ThenInclude(b => b.Card)
            .Include(p => p.Bill)
                .ThenInclude(b => b.Payments)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    // Get all payments made by this user, newest first
    public async Task<IEnumerable<Payment>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Payments
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();
    }

    // Idempotency check — find payment by unique transaction reference
    public async Task<Payment?> GetByTransactionReferenceAsync(string transactionReference)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.TransactionReference == transactionReference);
    }

    public async Task AddAsync(Payment payment)
    {
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();
    }
}
