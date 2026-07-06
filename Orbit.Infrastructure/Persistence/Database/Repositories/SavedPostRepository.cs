using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Orbit.Domain.Entities;
using Orbit.Domain.Interfaces.Repositories;
using Orbit.Domain.DataBase.Context;

namespace Orbit.Infrastructure.Persistence.Database.Repositories;

public class SavedPostRepository(OrbitDbContext context)
    : GenericRepository<SavedPost>(context), ISavedPostRepository
{
    public async Task<List<SavedPost>> GetListAsync(Expression<Func<SavedPost, bool>> predicate)
    {
        return await DbSet.Where(predicate).ToListAsync();
    }

    public async Task<int> CountAsync(Expression<Func<SavedPost, bool>> predicate)
    {
        return await DbSet.CountAsync(predicate);
    }

    public async Task<List<SavedPost>> GetPagedAsync<TKey>(
        Expression<Func<SavedPost, bool>> predicate,
        Expression<Func<SavedPost, TKey>> orderByDescending,
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
