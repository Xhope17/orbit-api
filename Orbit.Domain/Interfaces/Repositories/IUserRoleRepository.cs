using System.Linq.Expressions;
using Orbit.Domain.Entities;

namespace Orbit.Domain.Interfaces.Repositories;

public interface IUserRoleRepository : IGenericRepository<UserRole>
{
    Task<List<UserRole>> GetListAsync(Expression<Func<UserRole, bool>> predicate);
}
