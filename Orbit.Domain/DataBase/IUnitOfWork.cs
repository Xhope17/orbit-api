using Orbit.Domain.Interfaces.Repositories;

namespace Orbit.Domain.DataBase;

public interface IUnitOfWork
{
    IAuthUserRepository authUserRepository { get; }
    IProfileRepository profileRepository { get; }
    IPostRepository postRepository { get; }
    ICommentRepository commentRepository { get; }
    ICommentLikeRepository commentLikeRepository { get; }
    IPostLikeRepository postLikeRepository { get; }
    IPostMediaRepository postMediaRepository { get; }
    IFollowRepository followRepository { get; }
    ISavedPostRepository savedPostRepository { get; }
    IUserBanRepository userBanRepository { get; }
    IRoleRepository roleRepository { get; }
    IUserRoleRepository userRoleRepository { get; }
    IUserSessionRepository userSessionRepository { get; }
    IUserPrefixRepository userPrefixRepository { get; }
    IEmailTemplateRepository emailTemplateRepository { get; }
    ICommunityRepository communityRepository { get; }
    ICommunityMemberRepository communityMemberRepository { get; }
    ICommunityJoinRequestRepository communityJoinRequestRepository { get; }
    ICommunityInvitationRepository communityInvitationRepository { get; }
    IHashtagRepository hashtagRepository { get; }
    IPostHashtagRepository postHashtagRepository { get; }
    INotificationRepository notificationRepository { get; }
    Task SaveChangesAsync();
}
