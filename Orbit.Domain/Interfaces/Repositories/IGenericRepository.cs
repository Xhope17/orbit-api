using System.Linq.Expressions;

namespace Orbit.Domain.Interfaces.Repositories;

public interface IGenericRepository<T> where T : class
{
    Task<T> Create(T entity);
    Task<T> Update(T entity);
    Task<bool> Delete(T entity);
    Task<bool> IfExists(Expression<Func<T, bool>> expression);
    Task<T?> Get(Expression<Func<T, bool>> expression);
    IQueryable<T> Queryable();
}
