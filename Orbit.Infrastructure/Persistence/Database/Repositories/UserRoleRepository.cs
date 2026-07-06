using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Orbit.Domain.Entities;
using Orbit.Domain.Interfaces.Repositories;
using Orbit.Domain.DataBase.Context;

namespace Orbit.Infrastructure.Persistence.Database.Repositories;

public class UserRoleRepository(OrbitDbContext context)
    : GenericRepository<UserRole>(context), IUserRoleRepository
{
    public async Task<List<UserRole>> GetListAsync(Expression<Func<UserRole, bool>> predicate)
    {
        return await DbSet.Where(predicate).ToListAsync();
    }
}
