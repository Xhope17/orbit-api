using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Orbit.Domain.Entities;
using Orbit.Domain.Interfaces.Repositories;
using Orbit.Domain.DataBase.Context;

namespace Orbit.Infrastructure.Persistence.Database.Repositories;

public class ProfileRepository(OrbitDbContext context)
    : GenericRepository<Profile>(context), IProfileRepository
{
    public async Task<Profile?> GetByAuthUserId(Guid authUserId)
    {
        return await DbSet.FirstOrDefaultAsync(p => p.AuthUserId == authUserId);
    }

    public async Task<Profile?> GetByUsernameSlug(string slug)
    {
        return await DbSet.FirstOrDefaultAsync(p => p.UsernameSlug == slug);
    }

    public async Task<List<Profile>> GetListAsync(Expression<Func<Profile, bool>> predicate)
    {
        return await DbSet.Where(predicate).ToListAsync();
    }

    public async Task<int> CountAsync(Expression<Func<Profile, bool>> predicate)
    {
        return await DbSet.CountAsync(predicate);
    }

    public async Task<List<Profile>> GetPagedAsync<TKey>(
        Expression<Func<Profile, bool>> predicate,
        Expression<Func<Profile, TKey>> orderByDescending,
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
