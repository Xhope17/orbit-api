using System.Linq.Expressions;
using Orbit.Domain.Entities;

namespace Orbit.Domain.Interfaces.Repositories;

public interface IFollowRepository : IGenericRepository<Follow>
{
    Task<List<Follow>> GetListAsync(Expression<Func<Follow, bool>> predicate);
}
