using Identity.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Identity.Domain.Interfaces
{
    public interface IRoleRepository
    {
        Task<Role?> GetByNameAsync(string name);
        Task<Role?> GetByIdAsync(Guid id);
        Task<IEnumerable<Role>> GetAllAsync();
        Task AddAsync(Role role);
    }
}
