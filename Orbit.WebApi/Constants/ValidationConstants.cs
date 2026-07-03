namespace Orbit.WebApi.Constants;

public static class ValidationConstants
{
    // Auth - Email
    public const string EmailRequired = "Email is required";
    public const string EmailMaxLength = "Email must not exceed 255 characters";
    public const string EmailInvalidFormat = "Invalid email format";
    public const string EmailOrUsernameRequired = "Email or username is required";

    // Auth - Username
    public const string UsernameRequired = "Username is required";
    public const string UsernameLength = "Username must be between 3 and 30 characters";
    public const string UsernameInvalidChars = "Username can only contain letters, numbers and underscores";

    // Profile - DisplayName
    public const string DisplayNameRequired = "Display name is required";
    public const string DisplayNameMaxLength = "Display name must not exceed 100 characters";

    // Auth - Password
    public const string PasswordRequired = "Password is required";
    public const string NewPasswordRequired = "New password is required";
    public const string PasswordMinLength = "Password must be at least 8 characters";
    public const string PasswordUppercase = "Password must contain at least one uppercase letter";
    public const string PasswordNumber = "Password must contain at least one number";

    // Profile - Bio
    public const string BioMaxLength = "Bio must not exceed 500 characters";

    // Profile - Picture
    public const string PictureInvalidExtension = "Profile picture must be jpg, jpeg, png or webp";
    public const string PictureMaxSize = "Profile picture must not exceed 5MB";

    // Auth - Token
    public const string TokenRequired = "Token is required";
    public const string TokenLength = "Token must be 6 characters";

    // Post - Content
    public const string ContentRequired = "Content is required";
    public const string ContentRequiredAndMaxLength = "Content is required and must not exceed 1000 characters";
    public const string ContentMaxLength = "Content must not exceed 1000 characters";
    public const string ContentMaxLengthComment = "Content must not exceed 500 characters";

    // Post - Media
    public const string MediaInvalidExtension = "Each file must be jpg, jpeg, png, webp, gif, mp4, mov, avi or webm";
    public const string MediaMaxSize = "Each file must not exceed 10MB";
    public const string MediaMaxCount = "Maximum 10 files allowed per post";

    // Chat - Message
    public const string ContentMaxLengthMessage = "Content must not exceed 2000 characters";

    // Community
    public const string CommunityNameRequired = "Community name is required";
    public const string CommunityNameMaxLength = "Community name must not exceed 100 characters";
    public const string CommunityDescriptionMaxLength = "Community description must not exceed 500 characters";
}
