using Orbit.Domain.Common;

namespace Orbit.Domain.Entities;

public class UserPrefix : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Color { get; set; }
    public string? IconUrl { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Profile> Profiles { get; set; } = [];
}
