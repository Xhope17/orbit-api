using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Orbit.Domain.Common;
using Orbit.Domain.DataBase.Context;

namespace Orbit.Infrastructure.Persistence.Database.Repositories;

public class GenericRepository<T>(OrbitDbContext context) where T : class
{
    protected readonly OrbitDbContext Context = context;
    protected readonly DbSet<T> DbSet = context.Set<T>();

    public virtual async Task<T> Create(T entity)
    {
        await DbSet.AddAsync(entity);
        return entity;
    }

    public virtual async Task<T> Update(T entity)
    {
        DbSet.Update(entity);
        return entity;
    }

    public virtual async Task<bool> Delete(T entity)
    {
        if (entity is ISoftDeletable softDeletable)
        {
            softDeletable.IsActive = false;
            DbSet.Update(entity);
        }
        else
        {
            DbSet.Remove(entity);
        }
        return true;
    }

    public virtual async Task<bool> IfExists(Expression<Func<T, bool>> expression)
    {
        return await DbSet.AnyAsync(expression);
    }

    public virtual async Task<T?> Get(Expression<Func<T, bool>> expression)
    {
        return await DbSet.FirstOrDefaultAsync(expression);
    }

    public virtual IQueryable<T> Queryable()
    {
        var query = DbSet.AsQueryable();

        if (typeof(ISoftDeletable).IsAssignableFrom(typeof(T)))
        {
            query = query.Where(e => ((ISoftDeletable)e).IsActive);
        }

        return query;
    }
}
