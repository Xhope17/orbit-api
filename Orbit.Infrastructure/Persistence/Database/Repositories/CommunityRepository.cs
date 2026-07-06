using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Orbit.Domain.Entities;
using Orbit.Domain.Interfaces.Repositories;
using Orbit.Domain.DataBase.Context;

namespace Orbit.Infrastructure.Persistence.Database.Repositories;

public class CommunityRepository(OrbitDbContext context)
    : GenericRepository<Community>(context), ICommunityRepository
{
    public async Task<List<Community>> GetListAsync(Expression<Func<Community, bool>> predicate)
    {
        return await DbSet.Where(predicate).ToListAsync();
    }
}
