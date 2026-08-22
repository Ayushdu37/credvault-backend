using Identity.Domain.Entities;
using Identity.Domain.Interfaces;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories
{
    // Implements IUserRepository using EF Core to talk to SQL Server.
    // The Application layer only sees the interface — never EF Core directly.
    public class UserRepository : IUserRepository
    {
        private readonly IdentityDbContext _context;

        public UserRepository(IdentityDbContext context)
        {
            _context = context;
        }

        // .Include(u => u.Role) = Eager Loading — loads the related Role in the same query
        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        // Used during login (find user) and registration (check duplicate email)
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        // AddAsync tracks the entity, SaveChangesAsync sends the INSERT to the database
        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        // Update marks the entity as modified, SaveChangesAsync sends the UPDATE query
        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}
