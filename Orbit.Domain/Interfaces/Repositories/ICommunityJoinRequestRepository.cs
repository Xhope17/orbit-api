using System.Linq.Expressions;
using Orbit.Domain.Entities;

namespace Orbit.Domain.Interfaces.Repositories;

public interface ICommunityJoinRequestRepository : IGenericRepository<CommunityJoinRequest>
{
    Task<List<CommunityJoinRequest>> GetListAsync(Expression<Func<CommunityJoinRequest, bool>> predicate);
    Task<int> CountAsync(Expression<Func<CommunityJoinRequest, bool>> predicate);
    Task<List<CommunityJoinRequest>> GetPagedAsync<TKey>(
        Expression<Func<CommunityJoinRequest, bool>> predicate,
        Expression<Func<CommunityJoinRequest, TKey>> orderByDescending,
        int skip,
        int take);
}
