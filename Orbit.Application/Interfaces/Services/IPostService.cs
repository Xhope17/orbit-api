using Orbit.Application.Common;
using Orbit.Application.Models.DTOs;

namespace Orbit.Application.Interfaces.Services;

public interface IPostService
{
    Task<Result<PostResponse>> CreatePostAsync(Guid authUserId, string? content, List<MediaUploadData>? mediaFiles);
    Task<Result<PostResponse>> GetPostAsync(Guid postId, Guid? currentProfileId);
    Task<Result<PagedResult<PostResponse>>> GetGeneralPostsAsync(Guid? currentProfileId, int page, int pageSize);
    Task<Result<PagedResult<PostResponse>>> GetFollowingPostsAsync(Guid currentProfileId, int page, int pageSize);
    Task<Result<PagedResult<PostResponse>>> GetProfilePostsAsync(string username, Guid? currentProfileId, int page, int pageSize);
    Task<Result<PostResponse>> UpdatePostAsync(Guid authUserId, Guid postId, string? content, List<MediaUploadData>? mediaFiles = null);
    Task<Result> DeletePostAsync(Guid authUserId, Guid postId);
    Task<Result<LikeResponse>> LikePostAsync(Guid profileId, Guid postId);
    Task<Result<LikeResponse>> UnlikePostAsync(Guid profileId, Guid postId);
    Task<Result<PagedResult<PostResponse>>> SearchPostsAsync(string query, Guid? currentProfileId, int page, int pageSize);
    Task<Result<CommentResponse>> CreateCommentAsync(Guid profileId, Guid postId, string content, Guid? parentCommentId = null);
    Task<Result<PagedResult<CommentResponse>>> GetCommentsAsync(Guid postId, Guid? currentProfileId, int page, int pageSize);
    Task<Result<PagedResult<CommentResponse>>> GetCommentRepliesAsync(Guid commentId, Guid? currentProfileId, int page, int pageSize);
    Task<Result<CommentLikeResponse>> LikeCommentAsync(Guid profileId, Guid commentId);
    Task<Result<CommentLikeResponse>> UnlikeCommentAsync(Guid profileId, Guid commentId);
    Task<Result> DeleteCommentAsync(Guid authUserId, Guid commentId);
    Task<Result<SaveResponse>> SavePostAsync(Guid profileId, Guid postId);
    Task<Result<SaveResponse>> UnsavePostAsync(Guid profileId, Guid postId);
    Task<Result<PagedResult<PostResponse>>> GetSavedPostsAsync(Guid profileId, int page, int pageSize);
    Task<Result<PostResponse>> RepostPostAsync(Guid authUserId, Guid postId);
    Task<Result<PostResponse>> ThreadPostAsync(Guid authUserId, Guid postId, string content);
}
