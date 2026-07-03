using Microsoft.EntityFrameworkCore;
using Orbit.Domain.Entities;

namespace Orbit.Domain.DataBase.Context;

public class OrbitDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public OrbitDbContext(DbContextOptions<OrbitDbContext> options) : base(options) { }

    public DbSet<AuthUser> AuthUsers => Set<AuthUser>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserPrefix> UserPrefixes => Set<UserPrefix>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostMedia> PostMedia => Set<PostMedia>();
    public DbSet<PostLike> PostLikes => Set<PostLike>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Follow> Follows => Set<Follow>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageMedia> MessageMedia => Set<MessageMedia>();
    public DbSet<CommentLike> CommentLikes => Set<CommentLike>();
    public DbSet<UserBan> UserBans => Set<UserBan>();
    public DbSet<SavedPost> SavedPosts => Set<SavedPost>();
    public DbSet<Community> Communities => Set<Community>();
    public DbSet<CommunityMember> CommunityMembers => Set<CommunityMember>();
    public DbSet<CommunityJoinRequest> CommunityJoinRequests => Set<CommunityJoinRequest>();
    public DbSet<CommunityInvitation> CommunityInvitations => Set<CommunityInvitation>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Hashtag> Hashtags => Set<Hashtag>();
    public DbSet<PostHashtag> PostHashtags => Set<PostHashtag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrbitDbContext).Assembly);
    }
}
