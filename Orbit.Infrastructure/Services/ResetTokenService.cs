using Microsoft.Extensions.Caching.Distributed;
using Orbit.Application.Interfaces.Services;

namespace Orbit.Infrastructure.Services;

public class ResetTokenService : IResetTokenService
{
    private readonly IDistributedCache _cache;

    private static string Key(string email) => $"pwd_reset:{email.ToLowerInvariant()}";

    public ResetTokenService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task SaveTokenAsync(string email, string token, TimeSpan expiration)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration,
        };

        await _cache.SetStringAsync(Key(email), token, options);
    }

    public async Task<string?> GetTokenAsync(string email)
    {
        return await _cache.GetStringAsync(Key(email));
    }

    public async Task RemoveTokenAsync(string email)
    {
        await _cache.RemoveAsync(Key(email));
    }
}
