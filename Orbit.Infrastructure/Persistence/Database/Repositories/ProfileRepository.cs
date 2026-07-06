using Microsoft.EntityFrameworkCore;
using Orbit.Domain.Entities;
using Orbit.Domain.Interfaces.Repositories;
using Orbit.Domain.DataBase.Context;

namespace Orbit.Infrastructure.Persistence.Database.Repositories;

public class ProfileRepository(OrbitDbContext context)
    : GenericRepository<Profile>(context), IProfileRepository
{
    public async Task<Profile?> GetByAuthUserId(Guid authUserId)
    {
        return await DbSet.FirstOrDefaultAsync(p => p.AuthUserId == authUserId);
    }

    public async Task<Profile?> GetByUsernameSlug(string slug)
    {
        return await DbSet.FirstOrDefaultAsync(p => p.UsernameSlug == slug);
    }
}
