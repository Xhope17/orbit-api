using Orbit.Domain.Entities;

namespace Orbit.Domain.Interfaces.Repositories;

public interface IProfileRepository : IGenericRepository<Profile>
{
    Task<Profile?> GetByAuthUserId(Guid authUserId);
    Task<Profile?> GetByUsernameSlug(string slug);
}
