using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Orbit.Domain.Entities;
using Orbit.Domain.Interfaces.Repositories;
using Orbit.Domain.DataBase.Context;

namespace Orbit.Infrastructure.Persistence.Database.Repositories;

public class FollowRepository(OrbitDbContext context)
    : GenericRepository<Follow>(context), IFollowRepository
{
    public async Task<List<Follow>> GetListAsync(Expression<Func<Follow, bool>> predicate)
    {
        return await DbSet.Where(predicate).ToListAsync();
    }
}
