using Orbit.Domain.Entities;

namespace Orbit.Domain.Interfaces.Repositories;

public interface IAuthUserRepository : IGenericRepository<AuthUser>
{
    Task<AuthUser?> GetByEmail(string email);
}
