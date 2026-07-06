using System.Linq.Expressions;
using Orbit.Domain.Entities;

namespace Orbit.Domain.Interfaces.Repositories;

public interface ICommunityMemberRepository : IGenericRepository<CommunityMember>
{
    Task<List<CommunityMember>> GetListAsync(Expression<Func<CommunityMember, bool>> predicate);
    Task<int> CountAsync(Expression<Func<CommunityMember, bool>> predicate);
    Task<List<CommunityMember>> GetPagedAsync<TKey>(
        Expression<Func<CommunityMember, bool>> predicate,
        Expression<Func<CommunityMember, TKey>> orderByDescending,
        int skip,
        int take);
}
