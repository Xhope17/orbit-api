using System.Linq.Expressions;
using Orbit.Domain.Entities;

namespace Orbit.Domain.Interfaces.Repositories;

public interface ICommunityRepository : IGenericRepository<Community>
{
    Task<List<Community>> GetListAsync(Expression<Func<Community, bool>> predicate);
    Task<int> CountAsync(Expression<Func<Community, bool>> predicate);
    Task<List<Community>> GetPagedAsync<TKey>(
        Expression<Func<Community, bool>> predicate,
        Expression<Func<Community, TKey>> orderByDescending,
        int skip,
        int take);
}
