using FluentValidation;
using Orbit.WebApi.Constants;
using Orbit.WebApi.Models;

namespace Orbit.WebApi.Validators;

public class RegisterValidator : AbstractValidator<RegisterRequest>
{
    private static readonly HashSet<string> AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxFileSize = 5 * 1024 * 1024;

    public RegisterValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(ValidationConstants.EmailRequired)
            .MaximumLength(255).WithMessage(ValidationConstants.EmailMaxLength)
            .EmailAddress().WithMessage(ValidationConstants.EmailInvalidFormat);

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage(ValidationConstants.UsernameRequired)
            .Length(3, 30).WithMessage(ValidationConstants.UsernameLength)
            .Matches("^[a-zA-Z0-9_]+$").WithMessage(ValidationConstants.UsernameInvalidChars);

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage(ValidationConstants.DisplayNameRequired)
            .MaximumLength(100).WithMessage(ValidationConstants.DisplayNameMaxLength);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(ValidationConstants.PasswordRequired)
            .MinimumLength(8).WithMessage(ValidationConstants.PasswordMinLength)
            .Matches("[A-Z]").WithMessage(ValidationConstants.PasswordUppercase)
            .Matches("[0-9]").WithMessage(ValidationConstants.PasswordNumber);

        RuleFor(x => x.Bio)
            .MaximumLength(500).WithMessage(ValidationConstants.BioMaxLength);

        When(x => x.ProfilePicture is not null, () =>
        {
            RuleFor(x => x.ProfilePicture!)
                .Must(BeValidExtension).WithMessage(ValidationConstants.PictureInvalidExtension)
                .Must(f => f.Length <= MaxFileSize).WithMessage(ValidationConstants.PictureMaxSize);
        });
    }

    private static bool BeValidExtension(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return AllowedExtensions.Contains(ext);
    }
}
