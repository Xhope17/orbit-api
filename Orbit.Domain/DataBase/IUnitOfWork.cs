using Orbit.Domain.Interfaces.Repositories;

namespace Orbit.Domain.DataBase;

public interface IUnitOfWork
{
    IAuthUserRepository AuthUserRepository { get; }
    IProfileRepository ProfileRepository { get; }
    IPostRepository PostRepository { get; }
    ICommentRepository CommentRepository { get; }
    ICommentLikeRepository CommentLikeRepository { get; }
    IPostLikeRepository PostLikeRepository { get; }
    IPostMediaRepository PostMediaRepository { get; }
    IFollowRepository FollowRepository { get; }
    ISavedPostRepository SavedPostRepository { get; }
    IUserBanRepository UserBanRepository { get; }
    IRoleRepository RoleRepository { get; }
    IUserRoleRepository UserRoleRepository { get; }
    IUserSessionRepository UserSessionRepository { get; }
    IUserPrefixRepository UserPrefixRepository { get; }
    IEmailTemplateRepository EmailTemplateRepository { get; }
    ICommunityRepository CommunityRepository { get; }
    ICommunityMemberRepository CommunityMemberRepository { get; }
    ICommunityJoinRequestRepository CommunityJoinRequestRepository { get; }
    ICommunityInvitationRepository CommunityInvitationRepository { get; }
    IHashtagRepository HashtagRepository { get; }
    IPostHashtagRepository PostHashtagRepository { get; }
    INotificationRepository NotificationRepository { get; }
    Task SaveChangesAsync();
}
