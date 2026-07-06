using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Orbit.Domain.Entities;
using Orbit.Domain.Interfaces.Repositories;
using Orbit.Domain.DataBase.Context;

namespace Orbit.Infrastructure.Persistence.Database.Repositories;

public class CommunityInvitationRepository(OrbitDbContext context)
    : GenericRepository<CommunityInvitation>(context), ICommunityInvitationRepository
{
    public async Task<List<CommunityInvitation>> GetListAsync(Expression<Func<CommunityInvitation, bool>> predicate)
    {
        return await DbSet.Where(predicate).ToListAsync();
    }

    public async Task<int> CountAsync(Expression<Func<CommunityInvitation, bool>> predicate)
    {
        return await DbSet.CountAsync(predicate);
    }

    public async Task<List<CommunityInvitation>> GetPagedAsync<TKey>(
        Expression<Func<CommunityInvitation, bool>> predicate,
        Expression<Func<CommunityInvitation, TKey>> orderByDescending,
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
