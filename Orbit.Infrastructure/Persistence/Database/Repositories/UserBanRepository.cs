using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Orbit.Domain.Entities;
using Orbit.Domain.Interfaces.Repositories;
using Orbit.Domain.DataBase.Context;

namespace Orbit.Infrastructure.Persistence.Database.Repositories;

public class UserBanRepository(OrbitDbContext context)
    : GenericRepository<UserBan>(context), IUserBanRepository
{
    public async Task<List<UserBan>> GetListAsync(Expression<Func<UserBan, bool>> predicate)
    {
        return await DbSet.Where(predicate).ToListAsync();
    }

    public async Task<int> CountAsync(Expression<Func<UserBan, bool>> predicate)
    {
        return await DbSet.CountAsync(predicate);
    }

    public async Task<List<UserBan>> GetPagedAsync<TKey>(
        Expression<Func<UserBan, bool>> predicate,
        Expression<Func<UserBan, TKey>> orderByDescending,
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
