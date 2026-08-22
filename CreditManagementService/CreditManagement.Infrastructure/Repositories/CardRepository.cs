using CreditManagement.Domain.Entities;
using CreditManagement.Domain.Interfaces;
using CreditManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CreditManagement.Infrastructure.Repositories;

// Implements ICardRepository using EF Core
public class CardRepository : ICardRepository
{
    private readonly CreditManagementDbContext _context;

    public CardRepository(CreditManagementDbContext context)
    {
        _context = context;
    }

    public async Task<Card?> GetByIdAsync(Guid id)
    {
        return await _context.Cards
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    // Get all cards for a specific user (scoped by JWT UserId claim)
    public async Task<IEnumerable<Card>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Cards
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    // Find card by SHA-256 hash — used for duplicate card detection
    public async Task<Card?> GetByCardHashAsync(string cardHash)
    {
        return await _context.Cards
            .FirstOrDefaultAsync(c => c.CardNumberHash == cardHash);
    }

    public async Task AddAsync(Card card)
    {
        await _context.Cards.AddAsync(card);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Card card)
    {
        _context.Cards.Update(card);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Card card)
    {
        _context.Cards.Remove(card);
        await _context.SaveChangesAsync();
    }
}
