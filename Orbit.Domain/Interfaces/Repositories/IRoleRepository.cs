using System.Linq.Expressions;
using Orbit.Domain.Entities;

namespace Orbit.Domain.Interfaces.Repositories;

public interface IRoleRepository : IGenericRepository<Role>
{
    Task<List<Role>> GetListAsync(Expression<Func<Role, bool>> predicate);
}
