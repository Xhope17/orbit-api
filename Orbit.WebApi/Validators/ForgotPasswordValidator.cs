using FluentValidation;
using Orbit.WebApi.Constants;
using Orbit.WebApi.Models;

namespace Orbit.WebApi.Validators;

public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.EmailOrUsername)
            .NotEmpty().WithMessage(ValidationConstants.EmailOrUsernameRequired)
            .MaximumLength(255).WithMessage(ValidationConstants.EmailMaxLength);
    }
}
