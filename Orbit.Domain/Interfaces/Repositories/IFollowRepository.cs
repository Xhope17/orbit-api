using System.Linq.Expressions;
using Orbit.Domain.Entities;

namespace Orbit.Domain.Interfaces.Repositories;

public interface IFollowRepository : IGenericRepository<Follow>
{
    Task<List<Follow>> GetListAsync(Expression<Func<Follow, bool>> predicate);
    Task<int> CountAsync(Expression<Func<Follow, bool>> predicate);
    Task<List<Follow>> GetPagedAsync<TKey>(
        Expression<Func<Follow, bool>> predicate,
        Expression<Func<Follow, TKey>> orderByDescending,
        int skip,
        int take);
}
