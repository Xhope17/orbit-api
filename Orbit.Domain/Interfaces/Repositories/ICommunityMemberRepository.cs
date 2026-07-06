using System.Linq.Expressions;
using Orbit.Domain.Entities;

namespace Orbit.Domain.Interfaces.Repositories;

public interface ICommunityMemberRepository : IGenericRepository<CommunityMember>
{
    Task<List<CommunityMember>> GetListAsync(Expression<Func<CommunityMember, bool>> predicate);
}
