using CreditManagement.Domain.Entities;
using CreditManagement.Domain.Interfaces;
using CreditManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CreditManagement.Infrastructure.Repositories;

// Implements IBillRepository using EF Core
public class BillRepository : IBillRepository
{
    private readonly CreditManagementDbContext _context;

    public BillRepository(CreditManagementDbContext context)
    {
        _context = context;
    }

    // Include Card and Payments for navigation in BillDetailsDto
    public async Task<Bill?> GetByIdAsync(Guid id)
    {
        return await _context.Bills
            .Include(b => b.Card)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    // Get all bills across all cards owned by this user
    public async Task<IEnumerable<Bill>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Bills
            .Include(b => b.Card)
            .Where(b => b.Card.UserId == userId)
            .OrderByDescending(b => b.GeneratedAt)
            .ToListAsync();
    }

    // Get all bills for a specific card
    public async Task<IEnumerable<Bill>> GetByCardIdAsync(Guid cardId)
    {
        return await _context.Bills
            .Where(b => b.CardId == cardId)
            .OrderByDescending(b => b.GeneratedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Bill bill)
    {
        await _context.Bills.AddAsync(bill);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Bill bill)
    {
        _context.Bills.Update(bill);
        await _context.SaveChangesAsync();
    }
}
