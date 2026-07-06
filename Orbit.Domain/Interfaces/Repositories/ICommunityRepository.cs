using System.Linq.Expressions;
using Orbit.Domain.Entities;

namespace Orbit.Domain.Interfaces.Repositories;

public interface ICommunityRepository : IGenericRepository<Community>
{
    Task<List<Community>> GetListAsync(Expression<Func<Community, bool>> predicate);
}
