using Orbit.Domain.Common;

namespace Orbit.Domain.Entities;

public class MessageMedia : BaseEntity
{
    public Guid MessageId { get; set; }
    public string MediaType { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string PublicId { get; set; } = null!;
    public int? Width { get; set; }
    public int? Height { get; set; }
    public double? DurationSeconds { get; set; }
    public string? MimeType { get; set; }
    public long? SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }

    public Message Message { get; set; } = null!;
}
