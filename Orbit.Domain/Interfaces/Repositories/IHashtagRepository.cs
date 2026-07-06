using System.Linq.Expressions;
using Orbit.Domain.Entities;

namespace Orbit.Domain.Interfaces.Repositories;

public interface IHashtagRepository : IGenericRepository<Hashtag>
{
    Task<List<Hashtag>> GetListAsync(Expression<Func<Hashtag, bool>> predicate);
}
