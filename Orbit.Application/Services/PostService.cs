using Orbit.Application.Common;
using Orbit.Application.Constants;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Enums;
using Orbit.Application.Interfaces.Services;
using Orbit.Domain.DataBase;
using Orbit.Domain.Entities;

namespace Orbit.Application.Services;

public class PostService : IPostService
{
    private readonly IUnitOfWork _uow;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly NotificationChannel _notificationChannel;
    private readonly IHashtagService _hashtagService;

    public PostService(
        IUnitOfWork uow,
        ICloudinaryService cloudinaryService,
        NotificationChannel notificationChannel,
        IHashtagService hashtagService)
    {
        _uow = uow;
        _cloudinaryService = cloudinaryService;
        _notificationChannel = notificationChannel;
        _hashtagService = hashtagService;
    }

    public async Task<Result<PostResponse>> CreatePostAsync(Guid authUserId, string? content, List<MediaUploadData>? mediaFiles)
    {
        var profile = await _uow.profileRepository.Get(p => p.AuthUserId == authUserId);
        if (profile is null)
            return Result<PostResponse>.Failure(ResponseMessages.ProfileNotFound);

        var post = new Orbit.Domain.Entities.Post
        {
            Id = Guid.NewGuid(),
            ProfileId = profile.Id,
            Content = content ?? string.Empty,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _uow.postRepository.Create(post);

        var mediaList = new List<PostMedia>();
        if (mediaFiles is not null && mediaFiles.Count > 0)
        {
            for (int i = 0; i < mediaFiles.Count; i++)
            {
                var media = mediaFiles[i];
                var ext = Path.GetExtension(media.FileName);
                var fileName = $"{profile.Id}_{Guid.NewGuid()}{ext}";
                var uploadResult = await _cloudinaryService.UploadAsync(media.FileStream, fileName, CloudinaryFolder.PostMedia);

                if (uploadResult.IsSuccess && uploadResult.Data is not null)
                {
                    var data = uploadResult.Data;
                    var postMedia = new PostMedia
                    {
                        Id = Guid.NewGuid(),
                        PostId = post.Id,
                        Url = data.Url,
                        PublicId = data.PublicId,
                        MediaType = GetMediaType(media.FileName) ?? "image",
                        Order = i,
                        Width = data.Width,
                        Height = data.Height,
                        SizeBytes = data.SizeBytes,
                        Format = data.Format,
                        DurationSeconds = data.DurationSeconds,
                        CreatedAt = DateTime.UtcNow,
                    };
                    mediaList.Add(postMedia);
                    await _uow.postMediaRepository.Create(postMedia);
                }
            }
        }

        profile.PostsCount++;
        profile.UpdatedAt = DateTime.UtcNow;
        await _uow.profileRepository.Update(profile);
        await _uow.SaveChangesAsync();

        await _hashtagService.ProcessPostHashtags(post.Id, content);

        var author = BuildAuthorResponse(profile);
        return Result<PostResponse>.Success(BuildPostResponse(post, author, false, false, mediaList));
    }

    public async Task<Result<PostResponse>> GetPostAsync(Guid postId, Guid? currentProfileId)
    {
        var post = await _uow.postRepository.GetWithProfile(postId);
        if (post?.Profile is null)
            return Result<PostResponse>.Failure(ResponseMessages.PostNotFound);

        if (currentProfileId.HasValue && currentProfileId.Value != post.Profile.Id)
        {
            var isBlocked = await _uow.userBanRepository.Get(b =>
                b.BlockerProfileId == post.Profile.Id && b.BlockedProfileId == currentProfileId.Value);
            if (isBlocked is not null)
                return Result<PostResponse>.Failure(ResponseMessages.PostNotFound);
        }

        bool isLiked = false;
        bool isSaved = false;
        bool isFollowing = false;
        if (currentProfileId.HasValue)
        {
            var like = await _uow.postLikeRepository.Get(l => l.ProfileId == currentProfileId.Value && l.PostId == postId);
            isLiked = like is not null;

            var saved = await _uow.savedPostRepository.Get(s => s.ProfileId == currentProfileId.Value && s.PostId == postId);
            isSaved = saved is not null;

            if (post.Profile.Id != currentProfileId.Value)
            {
                var follow = await _uow.followRepository.Get(f =>
                    f.FollowerId == currentProfileId.Value && f.FollowingId == post.Profile.Id);
                isFollowing = follow is not null;
            }
        }

        var media = await _uow.postMediaRepository.GetListAsync(m => m.PostId == postId);

        var author = BuildAuthorResponse(post.Profile, isFollowing);

        PostResponse? originalPostResponse = null;
        if (post.OriginalPostId.HasValue)
        {
            var originalPost = await _uow.postRepository.GetWithProfile(post.OriginalPostId.Value);
            if (originalPost?.Profile is not null)
            {
                var originalMedia = await _uow.postMediaRepository.GetListAsync(m => m.PostId == originalPost.Id);
                var originalAuthor = BuildAuthorResponse(originalPost.Profile);
                originalPostResponse = BuildPostResponse(originalPost, originalAuthor, false, false, originalMedia);
            }
        }

        return Result<PostResponse>.Success(BuildPostResponse(post, author, isLiked, isSaved, media, originalPostResponse));
    }

    public async Task<Result<PagedResult<PostResponse>>> GetGeneralPostsAsync(Guid? currentProfileId, int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;

        HashSet<Guid> blockerProfileIds = [];
        if (currentProfileId.HasValue)
            blockerProfileIds = await GetBlockerProfileIdsAsync(currentProfileId.Value);

        var posts = await _uow.postRepository.GetPagedAsync(
            p => p.CommunityId == null && !blockerProfileIds.Contains(p.ProfileId),
            p => p.CreatedAt,
            skip,
            pageSize);

        var totalCount = await _uow.postRepository.CountAsync(p => p.CommunityId == null && !blockerProfileIds.Contains(p.ProfileId));

        return await BuildPagedPostResponse(posts, totalCount, page, pageSize, currentProfileId);
    }

    public async Task<Result<PagedResult<PostResponse>>> GetFollowingPostsAsync(Guid currentProfileId, int page, int pageSize)
    {
        var follows = await _uow.followRepository.GetListAsync(f => f.FollowerId == currentProfileId);
        var followedProfileIds = follows.Select(f => f.FollowingId).ToHashSet();

        if (followedProfileIds.Count == 0)
        {
            return Result<PagedResult<PostResponse>>.Success(new PagedResult<PostResponse>
            {
                Items = [],
                TotalCount = 0,
                Page = page,
                PageSize = pageSize,
            });
        }

        var blockerProfileIds = await GetBlockerProfileIdsAsync(currentProfileId);
        followedProfileIds.ExceptWith(blockerProfileIds);

        if (followedProfileIds.Count == 0)
        {
            return Result<PagedResult<PostResponse>>.Success(new PagedResult<PostResponse>
            {
                Items = [],
                TotalCount = 0,
                Page = page,
                PageSize = pageSize,
            });
        }

        var skip = (page - 1) * pageSize;

        var posts = await _uow.postRepository.GetPagedAsync(
            p => p.CommunityId == null && followedProfileIds.Contains(p.ProfileId),
            p => p.CreatedAt,
            skip,
            pageSize);

        var totalCount = await _uow.postRepository.CountAsync(p =>
            p.CommunityId == null && followedProfileIds.Contains(p.ProfileId));

        return await BuildPagedPostResponse(posts, totalCount, page, pageSize, currentProfileId);
    }

    public async Task<Result<PagedResult<PostResponse>>> GetProfilePostsAsync(string username, Guid? currentProfileId, int page, int pageSize)
    {
        var slug = username.ToLowerInvariant();
        var profile = await _uow.profileRepository.Get(p => p.UsernameSlug == slug);
        if (profile is null)
            return Result<PagedResult<PostResponse>>.Failure(ResponseMessages.ProfileNotFound);

        if (currentProfileId.HasValue && currentProfileId.Value != profile.Id)
        {
            var isBlocked = await _uow.userBanRepository.Get(b =>
                b.BlockerProfileId == profile.Id && b.BlockedProfileId == currentProfileId.Value);
            if (isBlocked is not null)
                return Result<PagedResult<PostResponse>>.Failure(ResponseMessages.PostNotFound);
        }

        var skip = (page - 1) * pageSize;
        var posts = await _uow.postRepository.GetPagedAsync(
            p => p.ProfileId == profile.Id && p.CommunityId == null,
            p => p.CreatedAt,
            skip,
            pageSize);

        var totalCount = await _uow.postRepository.CountAsync(p => p.ProfileId == profile.Id && p.CommunityId == null);

        return await BuildPagedPostResponse(posts, totalCount, page, pageSize, currentProfileId);
    }

    public async Task<Result<PagedResult<PostResponse>>> SearchPostsAsync(string query, Guid? currentProfileId, int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;

        HashSet<Guid> blockerProfileIds = [];
        if (currentProfileId.HasValue)
            blockerProfileIds = await GetBlockerProfileIdsAsync(currentProfileId.Value);

        var posts = await _uow.postRepository.GetPagedAsync(
            p => p.Content.Contains(query) && !blockerProfileIds.Contains(p.ProfileId),
            p => p.CreatedAt,
            skip,
            pageSize);

        var totalCount = await _uow.postRepository.CountAsync(p => p.Content.Contains(query) && !blockerProfileIds.Contains(p.ProfileId));

        return await BuildPagedPostResponse(posts, totalCount, page, pageSize, currentProfileId);
    }

    public async Task<Result<PostResponse>> UpdatePostAsync(Guid authUserId, Guid postId, string? content, List<MediaUploadData>? mediaFiles = null)
    {
        var profile = await _uow.profileRepository.Get(p => p.AuthUserId == authUserId);
        if (profile is null)
            return Result<PostResponse>.Failure(ResponseMessages.ProfileNotFound);

        var post = await _uow.postRepository.Get(p => p.Id == postId && p.ProfileId == profile.Id);
        if (post is null)
            return Result<PostResponse>.Failure(ResponseMessages.PostNotFound);

        post.Content = content ?? string.Empty;
        post.UpdatedAt = DateTime.UtcNow;
        await _uow.postRepository.Update(post);

        if (mediaFiles is not null)
        {
            var existingMedia = await _uow.postMediaRepository.GetListAsync(m => m.PostId == postId);
            foreach (var m in existingMedia)
            {
                await _cloudinaryService.DeleteAsync(m.PublicId);
                await _uow.postMediaRepository.Delete(m);
            }

            for (int i = 0; i < mediaFiles.Count; i++)
            {
                var media = mediaFiles[i];
                var ext = Path.GetExtension(media.FileName);
                var fileName = $"{profile.Id}_{Guid.NewGuid()}{ext}";
                var uploadResult = await _cloudinaryService.UploadAsync(media.FileStream, fileName, CloudinaryFolder.PostMedia);

                if (uploadResult.IsSuccess && uploadResult.Data is not null)
                {
                    var data = uploadResult.Data;
                    var postMedia = new PostMedia
                    {
                        Id = Guid.NewGuid(),
                        PostId = post.Id,
                        Url = data.Url,
                        PublicId = data.PublicId,
                        MediaType = GetMediaType(media.FileName) ?? "image",
                        Order = i,
                        Width = data.Width,
                        Height = data.Height,
                        SizeBytes = data.SizeBytes,
                        Format = data.Format,
                        DurationSeconds = data.DurationSeconds,
                        CreatedAt = DateTime.UtcNow,
                    };
                    await _uow.postMediaRepository.Create(postMedia);
                }
            }
        }

        await _uow.SaveChangesAsync();

        await _hashtagService.ProcessPostHashtags(postId, content);

        var mediaList = await _uow.postMediaRepository.GetListAsync(m => m.PostId == postId);

        var author = BuildAuthorResponse(profile);
        return Result<PostResponse>.Success(BuildPostResponse(post, author, false, false, mediaList));
    }

    public async Task<Result> DeletePostAsync(Guid authUserId, Guid postId)
    {
        var profile = await _uow.profileRepository.Get(p => p.AuthUserId == authUserId);
        if (profile is null)
            return Result.Failure(ResponseMessages.ProfileNotFound);

        var isModeratorOrAdmin = false;
        var moderatorRole = await _uow.roleRepository.Get(r => r.Name == "moderator");
        var adminRole = await _uow.roleRepository.Get(r => r.Name == "admin");

        if (moderatorRole is not null)
        {
            var hasModerator = await _uow.userRoleRepository.Get(ur =>
                ur.ProfileId == profile.Id && ur.RoleId == moderatorRole.Id);
            if (hasModerator is not null) isModeratorOrAdmin = true;
        }
        if (!isModeratorOrAdmin && adminRole is not null)
        {
            var hasAdmin = await _uow.userRoleRepository.Get(ur =>
                ur.ProfileId == profile.Id && ur.RoleId == adminRole.Id);
            if (hasAdmin is not null) isModeratorOrAdmin = true;
        }

        var post = isModeratorOrAdmin
            ? await _uow.postRepository.Get(p => p.Id == postId)
            : await _uow.postRepository.Get(p => p.Id == postId && p.ProfileId == profile.Id);

        if (post is null)
            return Result.Failure(ResponseMessages.PostNotFound);

        var mediaList = await _uow.postMediaRepository.GetListAsync(m => m.PostId == postId);
        foreach (var media in mediaList)
        {
            await _cloudinaryService.DeleteAsync(media.PublicId);
            await _uow.postMediaRepository.Delete(media);
        }

        await _uow.postRepository.Delete(post);
        await _uow.SaveChangesAsync();

        var ownerProfile = isModeratorOrAdmin
            ? await _uow.profileRepository.Get(p => p.Id == post.ProfileId)
            : profile;

        if (ownerProfile is not null)
        {
            ownerProfile.PostsCount = Math.Max(0, ownerProfile.PostsCount - 1);
            ownerProfile.UpdatedAt = DateTime.UtcNow;
            await _uow.profileRepository.Update(ownerProfile);
        }
        await _uow.SaveChangesAsync();

        return Result.Success(ResponseMessages.PostDeleted);
    }

    public async Task<Result<LikeResponse>> LikePostAsync(Guid profileId, Guid postId)
    {
        var post = await _uow.postRepository.Get(p => p.Id == postId);
        if (post is null)
            return Result<LikeResponse>.Failure(ResponseMessages.PostNotFound);

        var existingLike = await _uow.postLikeRepository.Get(l => l.ProfileId == profileId && l.PostId == postId);
        if (existingLike is not null)
            return Result<LikeResponse>.Success(new LikeResponse(postId, true, post.LikeCount));

        var like = new PostLike
        {
            ProfileId = profileId,
            PostId = postId,
            CreatedAt = DateTime.UtcNow,
        };

        await _uow.postLikeRepository.Create(like);

        post.LikeCount++;
        post.UpdatedAt = DateTime.UtcNow;
        await _uow.postRepository.Update(post);
        await _uow.SaveChangesAsync();

        if (post.ProfileId != profileId)
        {
            await _notificationChannel.Channel.Writer.WriteAsync(new NotificationEvent(
                post.ProfileId, "like", profileId, postId, null, post.Content, null));
        }

        return Result<LikeResponse>.Success(new LikeResponse(postId, true, post.LikeCount));
    }

    public async Task<Result<LikeResponse>> UnlikePostAsync(Guid profileId, Guid postId)
    {
        var post = await _uow.postRepository.Get(p => p.Id == postId);
        if (post is null)
            return Result<LikeResponse>.Failure(ResponseMessages.PostNotFound);

        var like = await _uow.postLikeRepository.Get(l => l.ProfileId == profileId && l.PostId == postId);
        if (like is null)
            return Result<LikeResponse>.Success(new LikeResponse(postId, false, post.LikeCount));

        await _uow.postLikeRepository.Delete(like);
        await _uow.SaveChangesAsync();

        post.LikeCount = Math.Max(0, post.LikeCount - 1);
        post.UpdatedAt = DateTime.UtcNow;
        await _uow.postRepository.Update(post);
        await _uow.SaveChangesAsync();

        return Result<LikeResponse>.Success(new LikeResponse(postId, false, post.LikeCount));
    }

    public async Task<Result<SaveResponse>> SavePostAsync(Guid profileId, Guid postId)
    {
        var post = await _uow.postRepository.Get(p => p.Id == postId);
        if (post is null)
            return Result<SaveResponse>.Failure(ResponseMessages.PostNotFound);

        var existing = await _uow.savedPostRepository.Get(s => s.ProfileId == profileId && s.PostId == postId);
        if (existing is not null)
            return Result<SaveResponse>.Success(new SaveResponse(postId, true));

        var savedPost = new SavedPost
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            PostId = postId,
            CreatedAt = DateTime.UtcNow,
        };

        await _uow.savedPostRepository.Create(savedPost);
        await _uow.SaveChangesAsync();

        post.SaveCount++;
        post.UpdatedAt = DateTime.UtcNow;
        await _uow.postRepository.Update(post);
        await _uow.SaveChangesAsync();

        return Result<SaveResponse>.Success(new SaveResponse(postId, true));
    }

    public async Task<Result<SaveResponse>> UnsavePostAsync(Guid profileId, Guid postId)
    {
        var post = await _uow.postRepository.Get(p => p.Id == postId);
        if (post is null)
            return Result<SaveResponse>.Failure(ResponseMessages.PostNotFound);

        var savedPost = await _uow.savedPostRepository.Get(s => s.ProfileId == profileId && s.PostId == postId);
        if (savedPost is null)
            return Result<SaveResponse>.Success(new SaveResponse(postId, false));

        await _uow.savedPostRepository.Delete(savedPost);
        await _uow.SaveChangesAsync();

        post.SaveCount = Math.Max(0, post.SaveCount - 1);
        post.UpdatedAt = DateTime.UtcNow;
        await _uow.postRepository.Update(post);
        await _uow.SaveChangesAsync();

        return Result<SaveResponse>.Success(new SaveResponse(postId, false));
    }

    public async Task<Result<PagedResult<PostResponse>>> GetSavedPostsAsync(Guid profileId, int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;
        var savedPosts = await _uow.savedPostRepository.GetPagedAsync(
            s => s.ProfileId == profileId,
            s => s.CreatedAt,
            skip,
            pageSize);

        var totalCount = await _uow.savedPostRepository.CountAsync(s => s.ProfileId == profileId);

        if (savedPosts.Count == 0)
            return Result<PagedResult<PostResponse>>.Success(new PagedResult<PostResponse>
            {
                Items = [],
                TotalCount = 0,
                Page = page,
                PageSize = pageSize,
            });

        var postIds = savedPosts.Select(s => s.PostId).ToList();
        var posts = await _uow.postRepository.GetListAsync(p => postIds.Contains(p.Id));

        var postMap = posts.ToDictionary(p => p.Id);
        var orderedPosts = savedPosts
            .Select(s => postMap.GetValueOrDefault(s.PostId))
            .Where(p => p is not null)
            .Cast<Orbit.Domain.Entities.Post>()
            .ToList();

        return await BuildPagedPostResponse(orderedPosts, totalCount, page, pageSize, profileId);
    }

    public async Task<Result<PostResponse>> RepostPostAsync(Guid authUserId, Guid postId)
    {
        var profile = await _uow.profileRepository.Get(p => p.AuthUserId == authUserId);
        if (profile is null)
            return Result<PostResponse>.Failure(ResponseMessages.ProfileNotFound);

        var originalPost = await _uow.postRepository.GetWithProfile(postId);
        if (originalPost is null)
            return Result<PostResponse>.Failure(ResponseMessages.PostNotFound);

        if (originalPost.IsThread)
            return Result<PostResponse>.Failure(ResponseMessages.CannotRepostThread);

        var existingRepost = await _uow.postRepository.Get(p =>
            p.ProfileId == profile.Id && p.OriginalPostId == postId && p.IsRepost);
        if (existingRepost is not null)
            return Result<PostResponse>.Failure(ResponseMessages.AlreadyReposted);

        var repost = new Orbit.Domain.Entities.Post
        {
            Id = Guid.NewGuid(),
            ProfileId = profile.Id,
            Content = string.Empty,
            IsActive = true,
            IsRepost = true,
            OriginalPostId = postId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _uow.postRepository.Create(repost);

        profile.PostsCount++;
        profile.UpdatedAt = DateTime.UtcNow;
        await _uow.profileRepository.Update(profile);
        await _uow.SaveChangesAsync();

        var originalMedia = await _uow.postMediaRepository.GetListAsync(m => m.PostId == originalPost.Id);
        var originalAuthor = BuildAuthorResponse(originalPost.Profile);
        var originalPostResponse = BuildPostResponse(originalPost, originalAuthor, false, false, originalMedia);

        if (originalPost.ProfileId != profile.Id)
        {
            await _notificationChannel.Channel.Writer.WriteAsync(new NotificationEvent(
                originalPost.ProfileId, "repost", profile.Id, originalPost.Id, null, originalPost.Content, null));
        }

        var author = BuildAuthorResponse(profile);
        return Result<PostResponse>.Success(BuildPostResponse(repost, author, false, false, [], originalPostResponse));
    }

    public async Task<Result<PostResponse>> ThreadPostAsync(Guid authUserId, Guid postId, string content)
    {
        var profile = await _uow.profileRepository.Get(p => p.AuthUserId == authUserId);
        if (profile is null)
            return Result<PostResponse>.Failure(ResponseMessages.ProfileNotFound);

        var parentPost = await _uow.postRepository.GetWithProfile(postId);
        if (parentPost is null)
            return Result<PostResponse>.Failure(ResponseMessages.PostNotFound);

        var thread = new Orbit.Domain.Entities.Post
        {
            Id = Guid.NewGuid(),
            ProfileId = profile.Id,
            Content = content,
            IsActive = true,
            IsThread = true,
            OriginalPostId = postId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _uow.postRepository.Create(thread);

        profile.PostsCount++;
        profile.UpdatedAt = DateTime.UtcNow;
        await _uow.profileRepository.Update(profile);
        await _uow.SaveChangesAsync();

        var parentMedia = await _uow.postMediaRepository.GetListAsync(m => m.PostId == parentPost.Id);
        var parentAuthor = BuildAuthorResponse(parentPost.Profile);
        var parentPostResponse = BuildPostResponse(parentPost, parentAuthor, false, false, parentMedia);

        if (parentPost.ProfileId != profile.Id)
        {
            await _notificationChannel.Channel.Writer.WriteAsync(new NotificationEvent(
                parentPost.ProfileId, "thread", profile.Id, parentPost.Id, null, parentPost.Content, null));
        }

        var author = BuildAuthorResponse(profile);
        return Result<PostResponse>.Success(BuildPostResponse(thread, author, false, false, [], parentPostResponse));
    }

    public async Task<Result<CommentResponse>> CreateCommentAsync(Guid profileId, Guid postId, string content, Guid? parentCommentId = null)
    {
        var post = await _uow.postRepository.Get(p => p.Id == postId);
        if (post is null)
            return Result<CommentResponse>.Failure(ResponseMessages.PostNotFound);

        if (post.ProfileId != profileId)
        {
            var isBlocked = await _uow.userBanRepository.Get(b =>
                b.BlockerProfileId == post.ProfileId && b.BlockedProfileId == profileId);
            if (isBlocked is not null)
                return Result<CommentResponse>.Failure(ResponseMessages.NotAuthorized);
        }

        Comment? parentComment = null;
        if (parentCommentId.HasValue)
        {
            parentComment = await _uow.commentRepository.Get(c => c.Id == parentCommentId.Value);
            if (parentComment is null)
                return Result<CommentResponse>.Failure(ResponseMessages.ParentCommentNotFound);

            if (parentComment.PostId != postId)
                return Result<CommentResponse>.Failure(ResponseMessages.ParentCommentNotInSamePost);
        }

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            PostId = postId,
            Content = content,
            ParentCommentId = parentCommentId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _uow.commentRepository.Create(comment);

        post.CommentCount++;
        post.UpdatedAt = DateTime.UtcNow;
        await _uow.postRepository.Update(post);
        await _uow.SaveChangesAsync();

        if (parentComment is not null)
        {
            parentComment.ReplyCount++;
            await _uow.commentRepository.Update(parentComment);
            await _uow.SaveChangesAsync();
        }

        var profile = await _uow.profileRepository.Get(p => p.Id == profileId);

        if (post.ProfileId != profileId)
        {
            await _notificationChannel.Channel.Writer.WriteAsync(new NotificationEvent(
                post.ProfileId, "comment", profileId, postId, comment.Id, post.Content, comment.Content));
        }

        var author = profile is not null ? BuildAuthorResponse(profile) : new PostAuthorResponse(profileId, "Unknown", "Unknown", null, false);

        return Result<CommentResponse>.Success(new CommentResponse(comment.Id, author, comment.Content, comment.ParentCommentId, comment.ReplyCount, comment.LikeCount, false, comment.CreatedAt));
    }

    public async Task<Result<PagedResult<CommentResponse>>> GetCommentsAsync(Guid postId, Guid? currentProfileId, int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;
        var comments = await _uow.commentRepository.GetPagedAsync(
            c => c.PostId == postId && c.ParentCommentId == null,
            c => c.CreatedAt,
            skip,
            pageSize);

        var totalCount = await _uow.commentRepository.CountAsync(c => c.PostId == postId && c.ParentCommentId == null);

        var profileIds = comments.Select(c => c.ProfileId).Distinct().ToList();
        var profileMap = await BatchLoadProfilesAsync(profileIds);

        var likedCommentIds = await GetLikedCommentIds(comments, currentProfileId);

        HashSet<Guid> followedProfileIds = [];
        if (currentProfileId.HasValue && profileIds.Count > 0)
        {
            var follows = await _uow.followRepository.GetListAsync(f =>
                f.FollowerId == currentProfileId.Value && profileIds.Contains(f.FollowingId));
            followedProfileIds = follows.Select(f => f.FollowingId).ToHashSet();
        }

        var items = comments.Select(c =>
        {
            var p = profileMap.GetValueOrDefault(c.ProfileId);
            var isFollowing = currentProfileId.HasValue && followedProfileIds.Contains(c.ProfileId);
            var author = p is not null ? BuildAuthorResponse(p, isFollowing) : new PostAuthorResponse(c.ProfileId, "Unknown", "Unknown", null, false);
            return new CommentResponse(c.Id, author, c.Content, c.ParentCommentId, c.ReplyCount, c.LikeCount, likedCommentIds.Contains(c.Id), c.CreatedAt);
        }).ToList();

        return Result<PagedResult<CommentResponse>>.Success(new PagedResult<CommentResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    public async Task<Result<PagedResult<CommentResponse>>> GetCommentRepliesAsync(Guid commentId, Guid? currentProfileId, int page, int pageSize)
    {
        var parentComment = await _uow.commentRepository.Get(c => c.Id == commentId);
        if (parentComment is null)
            return Result<PagedResult<CommentResponse>>.Failure(ResponseMessages.ParentCommentNotFound);

        var skip = (page - 1) * pageSize;
        var replies = await _uow.commentRepository.GetPagedAsync(
            c => c.ParentCommentId == commentId,
            c => c.CreatedAt,
            skip,
            pageSize);

        var totalCount = await _uow.commentRepository.CountAsync(c => c.ParentCommentId == commentId);

        var profileIds = replies.Select(c => c.ProfileId).Distinct().ToList();
        var profileMap = await BatchLoadProfilesAsync(profileIds);

        var likedCommentIds = await GetLikedCommentIds(replies, currentProfileId);

        HashSet<Guid> followedProfileIds = [];
        if (currentProfileId.HasValue && profileIds.Count > 0)
        {
            var follows = await _uow.followRepository.GetListAsync(f =>
                f.FollowerId == currentProfileId.Value && profileIds.Contains(f.FollowingId));
            followedProfileIds = follows.Select(f => f.FollowingId).ToHashSet();
        }

        var items = replies.Select(c =>
        {
            var p = profileMap.GetValueOrDefault(c.ProfileId);
            var isFollowing = currentProfileId.HasValue && followedProfileIds.Contains(c.ProfileId);
            var author = p is not null ? BuildAuthorResponse(p, isFollowing) : new PostAuthorResponse(c.ProfileId, "Unknown", "Unknown", null, false);
            return new CommentResponse(c.Id, author, c.Content, c.ParentCommentId, c.ReplyCount, c.LikeCount, likedCommentIds.Contains(c.Id), c.CreatedAt);
        }).ToList();

        return Result<PagedResult<CommentResponse>>.Success(new PagedResult<CommentResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    public async Task<Result> DeleteCommentAsync(Guid authUserId, Guid commentId)
    {
        var profile = await _uow.profileRepository.Get(p => p.AuthUserId == authUserId);
        if (profile is null)
            return Result.Failure(ResponseMessages.ProfileNotFound);

        var comment = await _uow.commentRepository.Get(c => c.Id == commentId);
        if (comment is null)
            return Result.Failure(ResponseMessages.CommentNotFound);

        if (comment.ProfileId != profile.Id)
        {
            var post = await _uow.postRepository.Get(p => p.Id == comment.PostId);
            if (post is null || post.ProfileId != profile.Id)
                return Result.Failure(ResponseMessages.NotAuthorized);
        }

        var parentCommentId = comment.ParentCommentId;

        await _uow.commentRepository.Delete(comment);
        await _uow.SaveChangesAsync();

        var postEntity = await _uow.postRepository.Get(p => p.Id == comment.PostId);
        if (postEntity is not null)
        {
            postEntity.CommentCount = Math.Max(0, postEntity.CommentCount - 1);
            postEntity.UpdatedAt = DateTime.UtcNow;
            await _uow.postRepository.Update(postEntity);
            await _uow.SaveChangesAsync();
        }

        if (parentCommentId.HasValue)
        {
            var parentComment = await _uow.commentRepository.Get(c => c.Id == parentCommentId.Value);
            if (parentComment is not null)
            {
                parentComment.ReplyCount = Math.Max(0, parentComment.ReplyCount - 1);
                await _uow.commentRepository.Update(parentComment);
                await _uow.SaveChangesAsync();
            }
        }

        return Result.Success(ResponseMessages.CommentDeleted);
    }

    public async Task<Result<CommentLikeResponse>> LikeCommentAsync(Guid profileId, Guid commentId)
    {
        var comment = await _uow.commentRepository.Get(c => c.Id == commentId);
        if (comment is null)
            return Result<CommentLikeResponse>.Failure(ResponseMessages.CommentNotFound);

        var existingLike = await _uow.commentLikeRepository.Get(cl => cl.ProfileId == profileId && cl.CommentId == commentId);
        if (existingLike is not null)
            return Result<CommentLikeResponse>.Success(new CommentLikeResponse(commentId, true, comment.LikeCount));

        var like = new CommentLike
        {
            ProfileId = profileId,
            CommentId = commentId,
            CreatedAt = DateTime.UtcNow,
        };

        await _uow.commentLikeRepository.Create(like);

        comment.LikeCount++;
        await _uow.commentRepository.Update(comment);
        await _uow.SaveChangesAsync();

        return Result<CommentLikeResponse>.Success(new CommentLikeResponse(commentId, true, comment.LikeCount));
    }

    public async Task<Result<CommentLikeResponse>> UnlikeCommentAsync(Guid profileId, Guid commentId)
    {
        var comment = await _uow.commentRepository.Get(c => c.Id == commentId);
        if (comment is null)
            return Result<CommentLikeResponse>.Failure(ResponseMessages.CommentNotFound);

        var like = await _uow.commentLikeRepository.Get(cl => cl.ProfileId == profileId && cl.CommentId == commentId);
        if (like is null)
            return Result<CommentLikeResponse>.Success(new CommentLikeResponse(commentId, false, comment.LikeCount));

        await _uow.commentLikeRepository.Delete(like);
        await _uow.SaveChangesAsync();

        comment.LikeCount = Math.Max(0, comment.LikeCount - 1);
        await _uow.commentRepository.Update(comment);
        await _uow.SaveChangesAsync();

        return Result<CommentLikeResponse>.Success(new CommentLikeResponse(commentId, false, comment.LikeCount));
    }

    private async Task<HashSet<Guid>> GetBlockerProfileIdsAsync(Guid profileId)
    {
        var bans = await _uow.userBanRepository.GetListAsync(b => b.BlockedProfileId == profileId);
        return bans.Select(b => b.BlockerProfileId).ToHashSet();
    }

    private async Task<HashSet<Guid>> GetLikedCommentIds(List<Comment> comments, Guid? currentProfileId)
    {
        if (!currentProfileId.HasValue || comments.Count == 0)
            return [];

        var commentIds = comments.Select(c => c.Id).ToList();
        var likes = await _uow.commentLikeRepository.GetListAsync(cl =>
            cl.ProfileId == currentProfileId.Value && commentIds.Contains(cl.CommentId));
        return likes.Select(l => l.CommentId).ToHashSet();
    }

    private async Task<Result<PagedResult<PostResponse>>> BuildPagedPostResponse(
        List<Orbit.Domain.Entities.Post> posts, int totalCount, int page, int pageSize, Guid? currentProfileId)
    {
        var profileIds = posts.Select(p => p.ProfileId).Distinct().ToList();
        var profileMap = await BatchLoadProfilesAsync(profileIds);

        HashSet<Guid> likedPostIds = [];
        HashSet<Guid> savedPostIds = [];
        HashSet<Guid> followedProfileIds = [];
        if (currentProfileId.HasValue && posts.Count > 0)
        {
            var postIds = posts.Select(p => p.Id).ToList();
            var likes = await _uow.postLikeRepository.GetListAsync(l =>
                l.ProfileId == currentProfileId.Value && postIds.Contains(l.PostId));
            likedPostIds = likes.Select(l => l.PostId).ToHashSet();

            var saved = await _uow.savedPostRepository.GetListAsync(s =>
                s.ProfileId == currentProfileId.Value && postIds.Contains(s.PostId));
            savedPostIds = saved.Select(s => s.PostId).ToHashSet();

            var follows = await _uow.followRepository.GetListAsync(f =>
                f.FollowerId == currentProfileId.Value && profileIds.Contains(f.FollowingId));
            followedProfileIds = follows.Select(f => f.FollowingId).ToHashSet();
        }

        Dictionary<Guid, List<PostMedia>> mediaMap = [];
        if (posts.Count > 0)
        {
            var postIds = posts.Select(p => p.Id).ToList();
            var allMedia = await _uow.postMediaRepository.GetListAsync(m => postIds.Contains(m.PostId));
            mediaMap = allMedia.GroupBy(m => m.PostId).ToDictionary(g => g.Key, g => g.ToList());
        }

        Dictionary<Guid, PostResponse> originalPostMap = [];
        var originalPostIds = posts
            .Where(p => p.OriginalPostId.HasValue)
            .Select(p => p.OriginalPostId!.Value)
            .Distinct()
            .ToList();

        if (originalPostIds.Count > 0)
        {
            var originalPosts = await _uow.postRepository.GetListAsync(p => originalPostIds.Contains(p.Id));
            var originalProfileIds = originalPosts.Select(p => p.ProfileId).Distinct().ToList();
            var originalProfileMap = await BatchLoadProfilesAsync(originalProfileIds);

            Dictionary<Guid, List<PostMedia>> originalMediaMap = [];
            if (originalPosts.Count > 0)
            {
                var origPostIds = originalPosts.Select(p => p.Id).ToList();
                var allOrigMedia = await _uow.postMediaRepository.GetListAsync(m => origPostIds.Contains(m.PostId));
                originalMediaMap = allOrigMedia.GroupBy(m => m.PostId).ToDictionary(g => g.Key, g => g.ToList());
            }

            foreach (var op in originalPosts)
            {
                if (originalProfileMap.TryGetValue(op.ProfileId, out var opProfile))
                {
                    var opAuthor = BuildAuthorResponse(opProfile);
                    var opMedia = originalMediaMap.GetValueOrDefault(op.Id) ?? [];
                    originalPostMap[op.Id] = BuildPostResponse(op, opAuthor, false, false, opMedia);
                }
            }
        }

        var items = posts.Select(p =>
        {
            var prof = profileMap.GetValueOrDefault(p.ProfileId);
            var isFollowing = currentProfileId.HasValue && followedProfileIds.Contains(p.ProfileId);
            var author = prof is not null
                ? BuildAuthorResponse(prof, isFollowing)
                : new PostAuthorResponse(p.ProfileId, "Unknown", "Unknown", null, false);
            var media = mediaMap.GetValueOrDefault(p.Id) ?? [];
            var originalPost = p.OriginalPostId.HasValue
                ? originalPostMap.GetValueOrDefault(p.OriginalPostId.Value)
                : null;
            return BuildPostResponse(p, author, likedPostIds.Contains(p.Id), savedPostIds.Contains(p.Id), media, originalPost);
        }).ToList();

        return Result<PagedResult<PostResponse>>.Success(new PagedResult<PostResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    private async Task<Dictionary<Guid, Profile>> BatchLoadProfilesAsync(List<Guid> profileIds)
    {
        if (profileIds.Count == 0) return [];
        var profiles = await _uow.profileRepository.GetListAsync(p => profileIds.Contains(p.Id));
        return profiles.ToDictionary(p => p.Id);
    }

    private static PostAuthorResponse BuildAuthorResponse(Profile profile, bool isFollowing = false)
    {
        return new PostAuthorResponse(
            profile.Id,
            profile.Username,
            profile.DisplayName,
            profile.ProfilePictureUrl,
            isFollowing
        );
    }

    private static PostResponse BuildPostResponse(Orbit.Domain.Entities.Post post, PostAuthorResponse author, bool isLiked, bool isSaved, List<PostMedia> media, PostResponse? originalPost = null)
    {
        return new PostResponse(
            post.Id,
            author,
            post.Content,
            media.OrderBy(m => m.Order).Select(m => new PostMediaResponse(
                m.Url,
                m.MediaType,
                m.Order,
                m.Width,
                m.Height,
                m.SizeBytes,
                m.Format,
                m.DurationSeconds
            )).ToList(),
            post.LikeCount,
            post.CommentCount,
            post.SaveCount,
            isLiked,
            isSaved,
            post.CreatedAt,
            post.UpdatedAt,
            post.IsRepost,
            post.IsThread,
            post.OriginalPostId,
            originalPost
        );
    }

    private static string? GetMediaType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif" => "image",
            ".mp4" or ".mov" or ".avi" or ".webm" => "video",
            _ => null
        };
    }
}
