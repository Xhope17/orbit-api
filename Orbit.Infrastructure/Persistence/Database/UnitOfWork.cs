using Orbit.Domain.DataBase;
using Orbit.Domain.DataBase.Context;
using Orbit.Domain.Interfaces.Repositories;

namespace Orbit.Infrastructure.Persistence.Database;

public class UnitOfWork(OrbitDbContext _context,
    IAuthUserRepository _authUserRepository,
    IProfileRepository _profileRepository,
    IPostRepository _postRepository,
    ICommentRepository _commentRepository,
    ICommentLikeRepository _commentLikeRepository,
    IPostLikeRepository _postLikeRepository,
    IPostMediaRepository _postMediaRepository,
    IFollowRepository _followRepository,
    ISavedPostRepository _savedPostRepository,
    IUserBanRepository _userBanRepository,
    IRoleRepository _roleRepository,
    IUserRoleRepository _userRoleRepository,
    IUserSessionRepository _userSessionRepository,
    IUserPrefixRepository _userPrefixRepository,
    IEmailTemplateRepository _emailTemplateRepository,
    ICommunityRepository _communityRepository,
    ICommunityMemberRepository _communityMemberRepository,
    ICommunityJoinRequestRepository _communityJoinRequestRepository,
    ICommunityInvitationRepository _communityInvitationRepository,
    IHashtagRepository _hashtagRepository,
    IPostHashtagRepository _postHashtagRepository,
    INotificationRepository _notificationRepository)
    : IUnitOfWork
{
    private readonly OrbitDbContext context = _context;
    public IAuthUserRepository authUserRepository { get; set; } = _authUserRepository;
    public IProfileRepository profileRepository { get; set; } = _profileRepository;
    public IPostRepository postRepository { get; set; } = _postRepository;
    public ICommentRepository commentRepository { get; set; } = _commentRepository;
    public ICommentLikeRepository commentLikeRepository { get; set; } = _commentLikeRepository;
    public IPostLikeRepository postLikeRepository { get; set; } = _postLikeRepository;
    public IPostMediaRepository postMediaRepository { get; set; } = _postMediaRepository;
    public IFollowRepository followRepository { get; set; } = _followRepository;
    public ISavedPostRepository savedPostRepository { get; set; } = _savedPostRepository;
    public IUserBanRepository userBanRepository { get; set; } = _userBanRepository;
    public IRoleRepository roleRepository { get; set; } = _roleRepository;
    public IUserRoleRepository userRoleRepository { get; set; } = _userRoleRepository;
    public IUserSessionRepository userSessionRepository { get; set; } = _userSessionRepository;
    public IUserPrefixRepository userPrefixRepository { get; set; } = _userPrefixRepository;
    public IEmailTemplateRepository emailTemplateRepository { get; set; } = _emailTemplateRepository;
    public ICommunityRepository communityRepository { get; set; } = _communityRepository;
    public ICommunityMemberRepository communityMemberRepository { get; set; } = _communityMemberRepository;
    public ICommunityJoinRequestRepository communityJoinRequestRepository { get; set; } = _communityJoinRequestRepository;
    public ICommunityInvitationRepository communityInvitationRepository { get; set; } = _communityInvitationRepository;
    public IHashtagRepository hashtagRepository { get; set; } = _hashtagRepository;
    public IPostHashtagRepository postHashtagRepository { get; set; } = _postHashtagRepository;
    public INotificationRepository notificationRepository { get; set; } = _notificationRepository;

    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }
}
