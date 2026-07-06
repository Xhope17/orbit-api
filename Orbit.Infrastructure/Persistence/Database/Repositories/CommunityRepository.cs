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

    public async Task<int> CountAsync(Expression<Func<Community, bool>> predicate)
    {
        return await DbSet.CountAsync(predicate);
    }

    public async Task<List<Community>> GetPagedAsync<TKey>(
        Expression<Func<Community, bool>> predicate,
        Expression<Func<Community, TKey>> orderByDescending,
        int skip,
        int take)
    {
        return await DbSet
            .Where(predicate)
            .OrderByDescending(orderByDescending)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }
}
