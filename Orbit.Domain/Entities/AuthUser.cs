using Orbit.Domain.Common;

namespace Orbit.Domain.Entities;

public class AuthUser : BaseEntity, ISoftDeletable
{
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public bool IsEmailVerified { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Profile Profile { get; set; } = null!;
    public ICollection<UserSession> UserSessions { get; set; } = [];
}
