using Orbit.Domain.Common;

namespace Orbit.Domain.Entities;

public class PostMedia : BaseEntity
{
    public Guid PostId { get; set; }
    public string Url { get; set; } = null!;
    public string PublicId { get; set; } = null!;
    public string MediaType { get; set; } = null!;
    public int Order { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public long? SizeBytes { get; set; }
    public string? Format { get; set; }
    public int? DurationSeconds { get; set; }
    public DateTime CreatedAt { get; set; }

    public Post Post { get; set; } = null!;
}
