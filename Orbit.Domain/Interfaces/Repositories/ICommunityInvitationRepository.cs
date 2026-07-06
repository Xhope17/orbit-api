using System.Linq.Expressions;
using Orbit.Domain.Entities;

namespace Orbit.Domain.Interfaces.Repositories;

public interface ICommunityInvitationRepository : IGenericRepository<CommunityInvitation>
{
    Task<List<CommunityInvitation>> GetListAsync(Expression<Func<CommunityInvitation, bool>> predicate);
    Task<int> CountAsync(Expression<Func<CommunityInvitation, bool>> predicate);
    Task<List<CommunityInvitation>> GetPagedAsync<TKey>(
        Expression<Func<CommunityInvitation, bool>> predicate,
        Expression<Func<CommunityInvitation, TKey>> orderByDescending,
        int skip,
        int take);
}
