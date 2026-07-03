namespace Orbit.Application.Constants;

public static class ResponseMessages
{
    public const string EmailAlreadyRegistered = "Email is already registered";
    public const string UsernameAlreadyTaken = "Username is already taken";
    public const string RegistrationSuccessful = "Registration successful";

    public const string InvalidCredentials = "Invalid credentials";
    public const string LoginSuccessful = "Login successful";

    public const string LoggedOutSuccessfully = "Logged out successfully";

    public const string TokenRefreshed = "Token refreshed successfully";
    public const string InvalidRefreshToken = "Invalid refresh token";
    public const string SessionExpired = "Session expired";
    public const string InvalidOrExpiredToken = "Invalid or expired token";

    public const string ProfileNotFound = "Profile not found";
    public const string FailedToUploadProfilePicture = "Failed to upload profile picture";
    public const string FailedToUploadBanner = "Failed to upload banner";

    public const string CheckYourInbox = "If registered, check your inbox";
    public const string PasswordResetSuccessful = "Password reset successful";

    public const string WelcomeEmailSent = "Welcome email sent";

    public const string ValidationFailed = "Validation failed";
    public const string InvalidToken = "Invalid token";
    public const string FileRequired = "File is required";

    public const string PostNotFound = "Post not found";
    public const string PostDeleted = "Post deleted successfully";
    public const string PostUpdated = "Post updated successfully";
    public const string CommentNotFound = "Comment not found";
    public const string CommentDeleted = "Comment deleted successfully";
    public const string NotAuthorized = "Not authorized";

    public const string CannotFollowYourself = "Cannot follow yourself";
    public const string AlreadyFollowing = "Already following this user";
    public const string NotFollowing = "You are not following this user";
    public const string FollowSuccessful = "Follow successful";
    public const string UnfollowSuccessful = "Unfollow successful";
    public const string CannotFollowBlockedByUser = "You cannot follow this user because they have blocked you";
    public const string CannotFollowBlockedUser = "You cannot follow a user you have blocked";

    // Chat
    public const string MutualFollowRequired = "Both users must follow each other to start a chat";
    public const string CannotChatYourself = "Cannot start a chat with yourself";
    public const string ConversationNotFound = "Conversation not found";
    public const string MessageNotFound = "Message not found";
    public const string MessageDeleted = "Message deleted successfully";
    public const string NotConversationParticipant = "You are not a participant in this conversation";
    public const string NotMessageOwner = "You can only delete your own messages";
    public const string MessageContentRequired = "Message content is required";
    public const string MessageContentMaxLength = "Message content must not exceed 2000 characters";

    // Roles
    public const string UserAlreadyModerator = "User is already a moderator";
    public const string UserNotModerator = "User is not a moderator";
    public const string OnlyAdminCanAssignRoles = "Only admins can assign roles";
    public const string RoleAssigned = "Role assigned successfully";
    public const string RoleRemoved = "Role removed successfully";

    // Ban
    public const string AccountBanned = "Your account has been banned";
    public const string AccountDeactivated = "Your account has been deactivated";
    public const string UserAlreadyBanned = "User is already banned";
    public const string UserNotBanned = "User is not banned";
    public const string BanSuccessful = "User banned successfully";
    public const string UnbanSuccessful = "User unbanned successfully";
    public const string CannotBanYourself = "Cannot ban yourself";
    public const string CannotBanAdmin = "Cannot ban an admin user";

    // Saved Posts
    public const string PostAlreadySaved = "Post is already saved";
    public const string PostNotSaved = "Post is not saved";
    public const string PostSaved = "Post saved successfully";
    public const string PostUnsaved = "Post unsaved successfully";

    // Moderator
    public const string NotAuthorizedModerator = "Only moderators or admins can perform this action";

    // Comments
    public const string ParentCommentNotFound = "Parent comment not found";
    public const string ParentCommentNotInSamePost = "Parent comment does not belong to this post";

    // Block
    public const string CannotBlockYourself = "Cannot block yourself";
    public const string AlreadyBlocked = "User is already blocked";
    public const string BlockedByUser = "Cannot block this user because they have blocked you";
    public const string NotBlocked = "User is not blocked";
    public const string BlockSuccessful = "User blocked successfully";
    public const string UnblockSuccessful = "User unblocked successfully";

    // Repost & Thread
    public const string CannotRepostYourself = "Cannot repost your own post";
    public const string CannotRepostThread = "Cannot repost a thread";
    public const string AlreadyReposted = "You have already reposted this post";
    public const string CannotThreadYourself = "Cannot create a thread on your own post";
    public const string RepostSuccessful = "Post reposted successfully";
    public const string ThreadCreated = "Thread created successfully";

    // Community
    public const string CommunityNotFound = "Community not found";
    public const string CommunityCreated = "Community created successfully";
    public const string CommunityUpdated = "Community updated successfully";
    public const string CommunityDeleted = "Community deleted successfully";
    public const string CommunityNameRequired = "Community name is required";
    public const string SlugAlreadyTaken = "This community name is already taken";
    public const string AlreadyMember = "You are already a member of this community";
    public const string NotMember = "You are not a member of this community";
    public const string CannotJoinPrivate = "This community is private. Use a join request instead";
    public const string JoinRequestSent = "Join request sent successfully";
    public const string JoinRequestAlreadyPending = "You already have a pending join request";
    public const string JoinRequestNotFound = "Join request not found";
    public const string JoinRequestApproved = "Join request approved";
    public const string JoinRequestRejected = "Join request rejected";
    public const string CannotApproveOwnRequest = "You cannot approve your own request";
    public const string InvitationSent = "Invitation sent successfully";
    public const string InvitationNotFound = "Invitation not found";
    public const string InvitationAlreadyPending = "User already has a pending invitation";
    public const string InvitationAccepted = "Invitation accepted";
    public const string InvitationDeclined = "Invitation declined";
    public const string CannotInviteYourself = "Cannot invite yourself";
    public const string AlreadyInvited = "User is already invited";
    public const string OwnerCannotLeave = "The owner cannot leave the community. Transfer ownership first or delete the community";
    public const string JoinSuccessful = "You joined the community";
    public const string LeaveSuccessful = "You left the community";
    public const string MemberKicked = "Member kicked successfully";
    public const string CannotKickOwner = "Cannot kick the owner of the community";
    public const string CoLeaderAssigned = "Co-leader assigned successfully";
    public const string CoLeaderRemoved = "Co-leader removed successfully";
    public const string AlreadyCoLeader = "User is already a co-leader";
    public const string NotCoLeader = "User is not a co-leader";
    public const string NoPermission = "You don't have permission to perform this action";
    public const string PrivateCommunityRequiresAuth = "Authentication is required to view members of a private community";
}
