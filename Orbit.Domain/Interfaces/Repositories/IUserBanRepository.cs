using System.Linq.Expressions;
using Orbit.Domain.Entities;

namespace Orbit.Domain.Interfaces.Repositories;

public interface IUserBanRepository : IGenericRepository<UserBan>
{
    Task<List<UserBan>> GetListAsync(Expression<Func<UserBan, bool>> predicate);
}
