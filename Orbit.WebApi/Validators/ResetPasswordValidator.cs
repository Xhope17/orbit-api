using FluentValidation;
using Orbit.WebApi.Constants;
using Orbit.WebApi.Models;

namespace Orbit.WebApi.Validators;

public class ResetPasswordValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage(ValidationConstants.UsernameRequired)
            .Length(3, 30).WithMessage(ValidationConstants.UsernameLength)
            .Matches("^[a-zA-Z0-9_]+$").WithMessage(ValidationConstants.UsernameInvalidChars);

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage(ValidationConstants.TokenRequired)
            .Length(6).WithMessage(ValidationConstants.TokenLength);

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage(ValidationConstants.NewPasswordRequired)
            .MinimumLength(8).WithMessage(ValidationConstants.PasswordMinLength)
            .Matches("[A-Z]").WithMessage(ValidationConstants.PasswordUppercase)
            .Matches("[0-9]").WithMessage(ValidationConstants.PasswordNumber);
    }
}
