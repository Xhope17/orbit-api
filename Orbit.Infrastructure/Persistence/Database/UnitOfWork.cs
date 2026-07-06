using Orbit.Domain.DataBase;
using Orbit.Domain.DataBase.Context;
using Orbit.Domain.Interfaces.Repositories;

namespace Orbit.Infrastructure.Persistence.Database;

public class UnitOfWork : IUnitOfWork
{
    private readonly OrbitDbContext _context;

    public UnitOfWork(
        OrbitDbContext context,
        IAuthUserRepository authUserRepository,
        IProfileRepository profileRepository,
        IPostRepository postRepository,
        ICommentRepository commentRepository,
        ICommentLikeRepository commentLikeRepository,
        IPostLikeRepository postLikeRepository,
        IPostMediaRepository postMediaRepository,
        IFollowRepository followRepository,
        ISavedPostRepository savedPostRepository,
        IUserBanRepository userBanRepository,
        IRoleRepository roleRepository,
        IUserRoleRepository userRoleRepository,
        IUserSessionRepository userSessionRepository,
        IUserPrefixRepository userPrefixRepository,
        IEmailTemplateRepository emailTemplateRepository,
        ICommunityRepository communityRepository,
        ICommunityMemberRepository communityMemberRepository,
        ICommunityJoinRequestRepository communityJoinRequestRepository,
        ICommunityInvitationRepository communityInvitationRepository,
        IHashtagRepository hashtagRepository,
        IPostHashtagRepository postHashtagRepository,
        INotificationRepository notificationRepository)
    {
        _context = context;
        AuthUserRepository = authUserRepository;
        ProfileRepository = profileRepository;
        PostRepository = postRepository;
        CommentRepository = commentRepository;
        CommentLikeRepository = commentLikeRepository;
        PostLikeRepository = postLikeRepository;
        PostMediaRepository = postMediaRepository;
        FollowRepository = followRepository;
        SavedPostRepository = savedPostRepository;
        UserBanRepository = userBanRepository;
        RoleRepository = roleRepository;
        UserRoleRepository = userRoleRepository;
        UserSessionRepository = userSessionRepository;
        UserPrefixRepository = userPrefixRepository;
        EmailTemplateRepository = emailTemplateRepository;
        CommunityRepository = communityRepository;
        CommunityMemberRepository = communityMemberRepository;
        CommunityJoinRequestRepository = communityJoinRequestRepository;
        CommunityInvitationRepository = communityInvitationRepository;
        HashtagRepository = hashtagRepository;
        PostHashtagRepository = postHashtagRepository;
        NotificationRepository = notificationRepository;
    }

    public IAuthUserRepository AuthUserRepository { get; }
    public IProfileRepository ProfileRepository { get; }
    public IPostRepository PostRepository { get; }
    public ICommentRepository CommentRepository { get; }
    public ICommentLikeRepository CommentLikeRepository { get; }
    public IPostLikeRepository PostLikeRepository { get; }
    public IPostMediaRepository PostMediaRepository { get; }
    public IFollowRepository FollowRepository { get; }
    public ISavedPostRepository SavedPostRepository { get; }
    public IUserBanRepository UserBanRepository { get; }
    public IRoleRepository RoleRepository { get; }
    public IUserRoleRepository UserRoleRepository { get; }
    public IUserSessionRepository UserSessionRepository { get; }
    public IUserPrefixRepository UserPrefixRepository { get; }
    public IEmailTemplateRepository EmailTemplateRepository { get; }
    public ICommunityRepository CommunityRepository { get; }
    public ICommunityMemberRepository CommunityMemberRepository { get; }
    public ICommunityJoinRequestRepository CommunityJoinRequestRepository { get; }
    public ICommunityInvitationRepository CommunityInvitationRepository { get; }
    public IHashtagRepository HashtagRepository { get; }
    public IPostHashtagRepository PostHashtagRepository { get; }
    public INotificationRepository NotificationRepository { get; }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
