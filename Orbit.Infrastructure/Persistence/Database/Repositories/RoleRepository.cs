using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Orbit.Domain.Entities;
using Orbit.Domain.Interfaces.Repositories;
using Orbit.Domain.DataBase.Context;

namespace Orbit.Infrastructure.Persistence.Database.Repositories;

public class RoleRepository(OrbitDbContext context)
    : GenericRepository<Role>(context), IRoleRepository
{
    public async Task<List<Role>> GetListAsync(Expression<Func<Role, bool>> predicate)
    {
        return await DbSet.Where(predicate).ToListAsync();
    }
}
