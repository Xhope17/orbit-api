using Microsoft.EntityFrameworkCore;
using Orbit.Domain.Entities;
using Orbit.Domain.Interfaces.Repositories;
using Orbit.Domain.DataBase.Context;

namespace Orbit.Infrastructure.Persistence.Database.Repositories;

public class AuthUserRepository(OrbitDbContext context)
    : GenericRepository<AuthUser>(context), IAuthUserRepository
{
    public async Task<AuthUser?> GetByEmail(string email)
    {
        return await DbSet.FirstOrDefaultAsync(u => u.Email == email);
    }
}
