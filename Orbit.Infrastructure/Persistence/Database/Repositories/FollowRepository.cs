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

    public async Task<int> CountAsync(Expression<Func<Follow, bool>> predicate)
    {
        return await DbSet.CountAsync(predicate);
    }

    public async Task<List<Follow>> GetPagedAsync<TKey>(
        Expression<Func<Follow, bool>> predicate,
        Expression<Func<Follow, TKey>> orderByDescending,
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
