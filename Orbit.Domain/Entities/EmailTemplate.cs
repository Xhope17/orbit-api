using Orbit.Domain.Common;

namespace Orbit.Domain.Entities;

public class EmailTemplate : BaseEntity, ISoftDeletable
{
    public string Name { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string HtmlBody { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
