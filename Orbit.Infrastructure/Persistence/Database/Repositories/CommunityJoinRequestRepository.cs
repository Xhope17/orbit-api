using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Orbit.Domain.Entities;
using Orbit.Domain.Interfaces.Repositories;
using Orbit.Domain.DataBase.Context;

namespace Orbit.Infrastructure.Persistence.Database.Repositories;

public class CommunityJoinRequestRepository(OrbitDbContext context)
    : GenericRepository<CommunityJoinRequest>(context), ICommunityJoinRequestRepository
{
    public async Task<List<CommunityJoinRequest>> GetListAsync(Expression<Func<CommunityJoinRequest, bool>> predicate)
    {
        return await DbSet.Where(predicate).ToListAsync();
    }

    public async Task<int> CountAsync(Expression<Func<CommunityJoinRequest, bool>> predicate)
    {
        return await DbSet.CountAsync(predicate);
    }

    public async Task<List<CommunityJoinRequest>> GetPagedAsync<TKey>(
        Expression<Func<CommunityJoinRequest, bool>> predicate,
        Expression<Func<CommunityJoinRequest, TKey>> orderByDescending,
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
