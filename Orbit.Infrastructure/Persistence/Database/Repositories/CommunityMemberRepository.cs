using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Orbit.Domain.Entities;
using Orbit.Domain.Interfaces.Repositories;
using Orbit.Domain.DataBase.Context;

namespace Orbit.Infrastructure.Persistence.Database.Repositories;

public class CommunityMemberRepository(OrbitDbContext context)
    : GenericRepository<CommunityMember>(context), ICommunityMemberRepository
{
    public async Task<List<CommunityMember>> GetListAsync(Expression<Func<CommunityMember, bool>> predicate)
    {
        return await DbSet.Where(predicate).ToListAsync();
    }

    public async Task<int> CountAsync(Expression<Func<CommunityMember, bool>> predicate)
    {
        return await DbSet.CountAsync(predicate);
    }

    public async Task<List<CommunityMember>> GetPagedAsync<TKey>(
        Expression<Func<CommunityMember, bool>> predicate,
        Expression<Func<CommunityMember, TKey>> orderByDescending,
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
