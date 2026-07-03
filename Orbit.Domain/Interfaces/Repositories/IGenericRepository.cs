using System.Linq.Expressions;
using Orbit.Domain.Common;

namespace Orbit.Domain.Interfaces.Repositories;

public interface IGenericRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<T?> GetByIdAsync(Guid id, params Expression<Func<T, object>>[] includes);
    Task<List<T>> GetAllAsync();
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
    Task<List<T>> GetListAsync(Expression<Func<T, bool>> predicate);
    Task CreateAsync(T entity);
    Task AddEntityAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
    Task DeleteAsync(Guid id);
    Task<int> CountAsync(Expression<Func<T, bool>> predicate);
    Task<List<T>> GetPagedAsync<TKey>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TKey>> orderByDescending,
        int skip,
        int take);
    Task SaveChangesAsync();
}
