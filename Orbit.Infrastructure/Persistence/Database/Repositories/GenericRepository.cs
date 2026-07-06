using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Orbit.Domain.Interfaces.Repositories;
using Orbit.Domain.Common;
using Orbit.Domain.DataBase.Context;

namespace Orbit.Infrastructure.Persistence.Database.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
{
    protected readonly OrbitDbContext Context;
    protected readonly DbSet<T> DbSet;

    public GenericRepository(OrbitDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        return await DbSet.FindAsync(id);
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, params Expression<Func<T, object>>[] includes)
    {
        var query = DbSet.AsQueryable();

        if (typeof(ISoftDeletable).IsAssignableFrom(typeof(T)))
            query = query.Where(e => ((ISoftDeletable)e).IsActive);

        query = includes.Aggregate(query, (current, include) => current.Include(include));

        return await query.FirstOrDefaultAsync(e => e.Id == id);
    }

    public virtual async Task<List<T>> GetAllAsync()
    {
        var query = DbSet.AsQueryable();

        if (typeof(ISoftDeletable).IsAssignableFrom(typeof(T)))
        {
            query = query.Where(e => ((ISoftDeletable)e).IsActive);
        }

        return await query.ToListAsync();
    }

    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        var query = DbSet.AsQueryable();

        if (typeof(ISoftDeletable).IsAssignableFrom(typeof(T)))
        {
            query = query.Where(e => ((ISoftDeletable)e).IsActive);
        }

        return await query.FirstOrDefaultAsync(predicate);
    }

    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
    {
        var query = DbSet.AsQueryable();

        if (typeof(ISoftDeletable).IsAssignableFrom(typeof(T)))
            query = query.Where(e => ((ISoftDeletable)e).IsActive);

        query = includes.Aggregate(query, (current, include) => current.Include(include));

        return await query.FirstOrDefaultAsync(predicate);
    }

    public virtual async Task<List<T>> GetListAsync(Expression<Func<T, bool>> predicate)
    {
        var query = DbSet.AsQueryable();

        if (typeof(ISoftDeletable).IsAssignableFrom(typeof(T)))
        {
            query = query.Where(e => ((ISoftDeletable)e).IsActive);
        }

        return await query.Where(predicate).ToListAsync();
    }

    public virtual async Task CreateAsync(T entity)
    {
        await DbSet.AddAsync(entity);
        await SaveChangesAsync();
    }

    public virtual async Task AddEntityAsync(T entity)
    {
        await DbSet.AddAsync(entity);
    }

    public virtual void Update(T entity)
    {
        DbSet.Update(entity);
    }

    public virtual void Remove(T entity)
    {
        DbSet.Remove(entity);
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
    {
        var query = DbSet.AsQueryable();

        if (typeof(ISoftDeletable).IsAssignableFrom(typeof(T)))
        {
            query = query.Where(e => ((ISoftDeletable)e).IsActive);
        }

        return await query.CountAsync(predicate);
    }

    public virtual async Task<List<T>> GetPagedAsync<TKey>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TKey>> orderByDescending,
        int skip,
        int take)
    {
        var query = DbSet.AsQueryable();

        if (typeof(ISoftDeletable).IsAssignableFrom(typeof(T)))
        {
            query = query.Where(e => ((ISoftDeletable)e).IsActive);
        }

        return await query
            .Where(predicate)
            .OrderByDescending(orderByDescending)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public virtual async Task DeleteAsync(Guid id)
    {
        var entity = await DbSet.FindAsync(id);

        if (entity is null) return;

        if (entity is ISoftDeletable softDeletable)
        {
            softDeletable.IsActive = false;
            DbSet.Update(entity);
        }
        else
        {
            DbSet.Remove(entity);
        }

        await SaveChangesAsync();
    }

    public virtual async Task SaveChangesAsync()
    {
        await Context.SaveChangesAsync();
    }
}
