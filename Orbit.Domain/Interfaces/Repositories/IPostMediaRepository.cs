using System.Linq.Expressions;
using Orbit.Domain.Entities;

namespace Orbit.Domain.Interfaces.Repositories;

public interface IPostMediaRepository : IGenericRepository<PostMedia>
{
    Task<List<PostMedia>> GetListAsync(Expression<Func<PostMedia, bool>> predicate);
}
