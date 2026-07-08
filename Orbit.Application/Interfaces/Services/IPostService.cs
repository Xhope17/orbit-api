using Orbit.Application.Common;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Models.Responses;

namespace Orbit.Application.Interfaces.Services;

public interface IPostService
{
    Task<Result<PostDto>> CreatePostAsync(Guid authUserId, string? content, List<MediaUploadData>? mediaFiles);
    Task<Result<PostDto>> GetPostAsync(Guid postId, Guid? currentProfileId);
    Task<Result<PagedResult<PostDto>>> GetGeneralPostsAsync(Guid? currentProfileId, int page, int pageSize);
    Task<Result<PagedResult<PostDto>>> GetFollowingPostsAsync(Guid currentProfileId, int page, int pageSize);
    Task<Result<PagedResult<PostDto>>> GetProfilePostsAsync(string username, Guid? currentProfileId, int page, int pageSize);
    Task<Result<PostDto>> UpdatePostAsync(Guid authUserId, Guid postId, string? content, List<MediaUploadData>? mediaFiles = null);
    Task<Result> DeletePostAsync(Guid authUserId, Guid postId);
    Task<Result<PostLikeResponse>> LikePostAsync(Guid profileId, Guid postId);
    Task<Result<PostLikeResponse>> UnlikePostAsync(Guid profileId, Guid postId);
    Task<Result<PagedResult<PostDto>>> SearchPostsAsync(string query, Guid? currentProfileId, int page, int pageSize);
    Task<Result<CommentDto>> CreateCommentAsync(Guid profileId, Guid postId, string content, Guid? parentCommentId = null);
    Task<Result<PagedResult<CommentDto>>> GetCommentsAsync(Guid postId, Guid? currentProfileId, int page, int pageSize);
    Task<Result<PagedResult<CommentDto>>> GetCommentRepliesAsync(Guid commentId, Guid? currentProfileId, int page, int pageSize);
    Task<Result<CommentLikeResponse>> LikeCommentAsync(Guid profileId, Guid commentId);
    Task<Result<CommentLikeResponse>> UnlikeCommentAsync(Guid profileId, Guid commentId);
    Task<Result> DeleteCommentAsync(Guid authUserId, Guid commentId);
    Task<Result<PostSaveResponse>> SavePostAsync(Guid profileId, Guid postId);
    Task<Result<PostSaveResponse>> UnsavePostAsync(Guid profileId, Guid postId);
    Task<Result<PagedResult<PostDto>>> GetSavedPostsAsync(Guid profileId, int page, int pageSize);
    Task<Result<PostDto>> RepostPostAsync(Guid authUserId, Guid postId);
    Task<Result<PostDto>> ThreadPostAsync(Guid authUserId, Guid postId, string content);
}
