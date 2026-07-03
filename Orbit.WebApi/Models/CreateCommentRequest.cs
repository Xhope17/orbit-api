namespace Orbit.WebApi.Models;

public record CreateCommentRequest(
    string Content,
    Guid? ParentCommentId = null
);
