using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Orbit.Domain.Entities;
using Orbit.Domain.Interfaces.Repositories;
using Orbit.Domain.DataBase.Context;

namespace Orbit.Infrastructure.Persistence.Database.Repositories;

public class CommentRepository(OrbitDbContext context)
    : GenericRepository<Comment>(context), ICommentRepository
{
    public async Task<List<Comment>> GetListAsync(Expression<Func<Comment, bool>> predicate)
    {
        return await DbSet.Where(predicate).ToListAsync();
    }

    public async Task<int> CountAsync(Expression<Func<Comment, bool>> predicate)
    {
        return await DbSet.CountAsync(predicate);
    }

    public async Task<List<Comment>> GetPagedAsync<TKey>(
        Expression<Func<Comment, bool>> predicate,
        Expression<Func<Comment, TKey>> orderByDescending,
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
