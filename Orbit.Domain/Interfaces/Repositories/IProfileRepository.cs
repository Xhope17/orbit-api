using System.Linq.Expressions;
using Orbit.Domain.Entities;

namespace Orbit.Domain.Interfaces.Repositories;

public interface IProfileRepository : IGenericRepository<Profile>
{
    Task<Profile?> GetByAuthUserId(Guid authUserId);
    Task<Profile?> GetByUsernameSlug(string slug);
    Task<List<Profile>> GetListAsync(Expression<Func<Profile, bool>> predicate);
    Task<int> CountAsync(Expression<Func<Profile, bool>> predicate);
    Task<List<Profile>> GetPagedAsync<TKey>(
        Expression<Func<Profile, bool>> predicate,
        Expression<Func<Profile, TKey>> orderByDescending,
        int skip,
        int take);
}
