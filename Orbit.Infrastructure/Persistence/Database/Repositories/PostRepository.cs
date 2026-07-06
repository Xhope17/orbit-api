using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Orbit.Domain.Common;
using Orbit.Domain.Entities;
using Orbit.Domain.Interfaces.Repositories;
using Orbit.Domain.DataBase.Context;

namespace Orbit.Infrastructure.Persistence.Database.Repositories;

public class PostRepository(OrbitDbContext context)
    : GenericRepository<Post>(context), IPostRepository
{
    public async Task<Post?> GetWithProfile(Guid id)
    {
        return await DbSet
            .Include(p => p.Profile)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Post>> GetListAsync(Expression<Func<Post, bool>> predicate)
    {
        return await DbSet.Where(predicate).ToListAsync();
    }

    public async Task<int> CountAsync(Expression<Func<Post, bool>> predicate)
    {
        return await DbSet.CountAsync(predicate);
    }

    public async Task<List<Post>> GetPagedAsync<TKey>(
        Expression<Func<Post, bool>> predicate,
        Expression<Func<Post, TKey>> orderByDescending,
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
