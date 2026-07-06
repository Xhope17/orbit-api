using System.Linq.Expressions;
using Orbit.Domain.Entities;

namespace Orbit.Domain.Interfaces.Repositories;

public interface IPostRepository : IGenericRepository<Post>
{
    Task<Post?> GetWithProfile(Guid id);
    Task<List<Post>> GetListAsync(Expression<Func<Post, bool>> predicate);
    Task<int> CountAsync(Expression<Func<Post, bool>> predicate);
    Task<List<Post>> GetPagedAsync<TKey>(
        Expression<Func<Post, bool>> predicate,
        Expression<Func<Post, TKey>> orderByDescending,
        int skip,
        int take);
}
