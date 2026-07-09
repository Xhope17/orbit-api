using Orbit.Application.Common;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Models.Responses;

namespace Orbit.Application.Interfaces.Services;

public interface IPostService
{
    Task<GenericResponse<PostDto>> CreatePostAsync(Guid authUserId, string? content, List<MediaUploadData>? mediaFiles);
    Task<GenericResponse<PostDto>> GetPostAsync(Guid postId, Guid? currentProfileId);
    Task<GenericResponse<PagedResult<PostDto>>> GetGeneralPostsAsync(Guid? currentProfileId, int page, int pageSize);
    Task<GenericResponse<PagedResult<PostDto>>> GetFollowingPostsAsync(Guid currentProfileId, int page, int pageSize);
    Task<GenericResponse<PagedResult<PostDto>>> GetProfilePostsAsync(string username, Guid? currentProfileId, int page, int pageSize);
    Task<GenericResponse<PostDto>> UpdatePostAsync(Guid authUserId, Guid postId, string? content, List<MediaUploadData>? mediaFiles = null);
    Task<GenericResponse<string>> DeletePostAsync(Guid authUserId, Guid postId);
    Task<GenericResponse<PostLikeResponse>> LikePostAsync(Guid profileId, Guid postId);
    Task<GenericResponse<PostLikeResponse>> UnlikePostAsync(Guid profileId, Guid postId);
    Task<GenericResponse<PagedResult<PostDto>>> SearchPostsAsync(string query, Guid? currentProfileId, int page, int pageSize);
    Task<GenericResponse<CommentDto>> CreateCommentAsync(Guid profileId, Guid postId, string content, Guid? parentCommentId = null);
    Task<GenericResponse<PagedResult<CommentDto>>> GetCommentsAsync(Guid postId, Guid? currentProfileId, int page, int pageSize);
    Task<GenericResponse<PagedResult<CommentDto>>> GetCommentRepliesAsync(Guid commentId, Guid? currentProfileId, int page, int pageSize);
    Task<GenericResponse<CommentLikeResponse>> LikeCommentAsync(Guid profileId, Guid commentId);
    Task<GenericResponse<CommentLikeResponse>> UnlikeCommentAsync(Guid profileId, Guid commentId);
    Task<GenericResponse<string>> DeleteCommentAsync(Guid authUserId, Guid commentId);
    Task<GenericResponse<PostSaveResponse>> SavePostAsync(Guid profileId, Guid postId);
    Task<GenericResponse<PostSaveResponse>> UnsavePostAsync(Guid profileId, Guid postId);
    Task<GenericResponse<PagedResult<PostDto>>> GetSavedPostsAsync(Guid profileId, int page, int pageSize);
    Task<GenericResponse<PostDto>> RepostPostAsync(Guid authUserId, Guid postId);
    Task<GenericResponse<PostDto>> ThreadPostAsync(Guid authUserId, Guid postId, string content);
}
