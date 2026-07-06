using System.Linq.Expressions;
using Orbit.Domain.Entities;

namespace Orbit.Domain.Interfaces.Repositories;

public interface IPostHashtagRepository : IGenericRepository<PostHashtag>
{
    Task<List<PostHashtag>> GetListAsync(Expression<Func<PostHashtag, bool>> predicate);
}
