using System.Linq.Expressions;
using Orbit.Domain.Entities;

namespace Orbit.Domain.Interfaces.Repositories;

public interface ISavedPostRepository : IGenericRepository<SavedPost>
{
    Task<List<SavedPost>> GetListAsync(Expression<Func<SavedPost, bool>> predicate);
    Task<int> CountAsync(Expression<Func<SavedPost, bool>> predicate);
    Task<List<SavedPost>> GetPagedAsync<TKey>(
        Expression<Func<SavedPost, bool>> predicate,
        Expression<Func<SavedPost, TKey>> orderByDescending,
        int skip,
        int take);
}
