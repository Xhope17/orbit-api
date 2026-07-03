using FluentValidation;
using Orbit.WebApi.Constants;
using Orbit.WebApi.Models;

namespace Orbit.WebApi.Validators;

public class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.EmailOrUsername)
            .NotEmpty().WithMessage(ValidationConstants.EmailOrUsernameRequired);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(ValidationConstants.PasswordRequired);
    }
}
