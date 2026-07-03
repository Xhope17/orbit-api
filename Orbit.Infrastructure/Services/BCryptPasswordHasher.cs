using Orbit.Application.Interfaces.Services;

namespace Orbit.Infrastructure.Services;

public class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.EnhancedHashPassword(password, 13);
    }

    public bool Verify(string password, string hash)
    {
        return BCrypt.Net.BCrypt.EnhancedVerify(password, hash);
    }
}
